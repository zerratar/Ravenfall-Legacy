// Exports a block of the map as JSON for Ravenfall's Ravebridge (a Lumbridge tribute): tile heights, the ground's
// underlays and overlays with their colours, and every loc (object) with its name, kind, place, turn and size.
// Usage: npx tsx scripts/cache/export-region.ts <baseX> <baseY> <sizeX> <sizeY> <out.json>
import fs from "fs";

import { CacheSystem } from "../../src/rs/cache/CacheSystem";
import { getCacheLoaderFactory } from "../../src/rs/cache/loader/CacheLoaderFactory";
import { ByteBuffer } from "../../src/rs/io/ByteBuffer";
import { LocModelLoader } from "../../src/rs/config/loctype/LocModelLoader";
import { Scene } from "../../src/rs/scene/Scene";
import { SceneBuilder } from "../../src/rs/scene/SceneBuilder";
import { loadCache, loadCacheInfos, loadCacheList } from "./load-util";
import { HSL_RGB_MAP } from "../../src/rs/util/ColorUtil";

const [baseX, baseY, sizeX, sizeY] = process.argv.slice(2, 6).map((v) => parseInt(v));
const out = process.argv[6] ?? "region.json";

const cacheList = loadCacheList(loadCacheInfos());
const loadedCache = loadCache(cacheList.latest);
const cacheSystem = CacheSystem.fromFiles(loadedCache.type, loadedCache.files);
const factory = getCacheLoaderFactory(cacheList.latest, cacheSystem);
const modelLoader = factory.getModelLoader();
const textureLoader = factory.getTextureLoader();

// a loc type's look in a few numbers: the average colour of its faces (recoloured, textures by their average) and
// how tall its model stands, for choosing a stand-in
const looks = new Map<number, any>();
function lookOf(t: any): any {
    if (looks.has(t.id)) return looks.get(t.id);
    let r = 0, g = 0, b = 0, n = 0, top = 0;
    for (const set of t.models ?? []) {
        for (const mid of set ?? []) {
            const m: any = modelLoader.getModel(mid);
            if (!m) continue;
            for (let v = 0; v < m.verticesCount; v++) top = Math.max(top, -m.verticesY[v]);
            for (let f = 0; f < m.faceCount; f++) {
                if (m.faceAlphas && (m.faceAlphas[f] & 0xff) > 200) continue;
                let hsl = m.faceColors[f];
                const tex = m.faceTextures ? m.faceTextures[f] : -1;
                if (t.recolorFrom) {
                    const i = t.recolorFrom.indexOf(hsl);
                    if (i >= 0) hsl = t.recolorTo[i];
                }
                if (tex >= 0) {
                    try { hsl = textureLoader.getAverageHsl(tex); } catch (e) {}
                }
                const rgb = HSL_RGB_MAP[hsl & 0xffff];
                r += (rgb >> 16) & 255; g += (rgb >> 8) & 255; b += rgb & 255; n++;
            }
        }
        break;
    }
    const look = n ? { rgb: [Math.round(r / n), Math.round(g / n), Math.round(b / n)], height: top } : { rgb: null, height: top };
    looks.set(t.id, look);
    return look;
}

const underlays = factory.getUnderlayTypeLoader();
const overlays = factory.getOverlayTypeLoader();
const locTypes = factory.getLocTypeLoader();
const mapFiles = factory.getMapFileLoader();
const locModels = new LocModelLoader(
    locTypes,
    factory.getModelLoader(),
    factory.getTextureLoader(),
    factory.getSeqTypeLoader(),
    factory.getSeqFrameLoader(),
    factory.getSkeletalSeqLoader(),
);
const builder = new SceneBuilder(
    loadedCache.info,
    mapFiles,
    underlays,
    overlays,
    locTypes,
    locModels,
    loadedCache.xteas,
);

const scene = new Scene(Scene.MAX_LEVELS, sizeX, sizeY);
const locs: any[] = [];
const S = Scene.MAP_SQUARE_SIZE;
for (let mx = Math.floor(baseX / S); mx < Math.ceil((baseX + sizeX) / S); mx++) {
    for (let my = Math.floor(baseY / S); my < Math.ceil((baseY + sizeY) / S); my++) {
        const offX = mx * S - baseX;
        const offY = my * S - baseY;
        const terrain = mapFiles.getTerrainData(mx, my);
        if (terrain) builder.decodeTerrain(scene, terrain, offX, offY, baseX, baseY);
        const data = mapFiles.getLocData(mx, my, loadedCache.xteas);
        if (!data) continue;
        // as SceneBuilder.decodeLocs, kept as data
        const buffer = new ByteBuffer(data);
        let id = -1;
        let idDelta: number;
        while ((idDelta = buffer.readSmart3()) !== 0) {
            id += idDelta;
            let pos = 0;
            let posDelta: number;
            while ((posDelta = buffer.readUnsignedSmart()) !== 0) {
                pos += posDelta - 1;
                const x = ((pos >> 6) & 0x3f) + offX;
                const y = (pos & 0x3f) + offY;
                const level = pos >> 12;
                const attributes = buffer.readUnsignedByte();
                if (x < 0 || y < 0 || x >= sizeX || y >= sizeY) continue;
                const t = locTypes.load(id);
                locs.push({
                    id,
                    name: t.name,
                    x,
                    y,
                    level,
                    type: attributes >> 2,
                    rot: attributes & 3,
                    sx: t.sizeX,
                    sy: t.sizeY,
                    scale: [t.modelSizeX, t.modelSizeHeight, t.modelSizeY],
                    mapFunction: t.mapFunctionId,
                    mapScene: t.mapSceneId,
                    clip: t.clipType,
                    look: lookOf(t),
                    actions: (t.actions ?? []).filter((a) => a),
                });
            }
        }
    }
}

const floorTypes: Record<string, any> = {};
// bridges: tiles whose level 1 is drawn a level down (the game's own rule for bridges over water), as [x, y]
const bridges: number[][] = [];
for (let x = 0; x < sizeX; x++)
    for (let y = 0; y < sizeY; y++)
        if ((scene.tileRenderFlags[1][x][y] & 2) === 2) bridges.push([x, y]);
// the upper floors (levels 1 to 3): every tile with ground drawn on it, as [x, y]
const floors: number[][][] = [[], [], [], []];
for (let level = 1; level < Scene.MAX_LEVELS; level++)
    for (let x = 0; x < sizeX; x++)
        for (let y = 0; y < sizeY; y++)
            if (scene.tileOverlays[level][x][y] > 0 || scene.tileUnderlays[level][x][y] > 0) floors[level].push([x, y]);
const heights: number[][] = [];
const underlay: number[][] = [];
const overlay: number[][] = [];
const shape: number[][] = [];
const rotation: number[][] = [];
const flags: number[][] = [];
for (let x = 0; x <= sizeX; x++) heights.push(Array.from(scene.tileHeights[0][x] ?? []));
for (let x = 0; x < sizeX; x++) {
    underlay.push(Array.from(scene.tileUnderlays[0][x]));
    overlay.push(Array.from(scene.tileOverlays[0][x]));
    shape.push(Array.from(scene.tileShapes[0][x]));
    rotation.push(Array.from(scene.tileRotations[0][x]));
    flags.push(Array.from(scene.tileRenderFlags[0][x]));
    for (let y = 0; y < sizeY; y++) {
        const u = scene.tileUnderlays[0][x][y];
        const o = scene.tileOverlays[0][x][y];
        if (u > 0 && !floorTypes["u" + u]) {
            const t: any = underlays.load(u - 1);
            floorTypes["u" + u] = { rgb: t.rgbColor, texture: t.textureId };
        }
        if (o > 0 && !floorTypes["o" + o]) {
            const t: any = overlays.load(o - 1 < 0 ? 0 : o - 1);
            floorTypes["o" + o] = {
                rgb: t.primaryRgb,
                secondary: t.secondaryRgb,
                texture: t.textureId,
                hideUnderlay: t.hideUnderlay,
            };
        }
    }
}

fs.writeFileSync(
    out,
    JSON.stringify({ baseX, baseY, sizeX, sizeY, floors, bridges, heights, underlay, overlay, shape, rotation, flags, floorTypes, locs }),
);
console.log("Wrote " + out + ": " + locs.length + " locs, " + Object.keys(floorTypes).length + " floor types");
