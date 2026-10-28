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
                    // the loc's own texture swaps (one roof model, red tiles here, slate there)
                    let tx = tex;
                    if (t.retextureFrom) {
                        const ri = t.retextureFrom.indexOf(tex);
                        if (ri >= 0) tx = t.retextureTo[ri];
                    }
                    try { hsl = textureLoader.getAverageHsl(tx); } catch (e) {}
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

// a roof piece's shape as four numbers: its model's height (game units, up) at the tile's south west, north west,
// north east and south east corners, turned as placed; the model's x runs east and its z north, 128 a tile
function roofCorners(t: any, type: number, rot: number): number[] | undefined {
    let m: any;
    try { m = locModels.getLocModelData(t, type, rot); } catch (e) { return undefined; }
    if (!m || !m.verticesCount) return undefined;
    // only the roof's surface: vertices of faces that are not upright (the model also has the walls and gable
    // triangles under it)
    const surface = new Set<number>();
    for (let f = 0; f < m.faceCount; f++) {
        const a = m.indices1[f], b = m.indices2[f], c = m.indices3[f];
        const ux = m.verticesX[b] - m.verticesX[a], uy = m.verticesY[b] - m.verticesY[a], uz = m.verticesZ[b] - m.verticesZ[a];
        const vx = m.verticesX[c] - m.verticesX[a], vy = m.verticesY[c] - m.verticesY[a], vz = m.verticesZ[c] - m.verticesZ[a];
        const nx = uy * vz - uz * vy, ny = uz * vx - ux * vz, nz = ux * vy - uy * vx;
        const len = Math.sqrt(nx * nx + ny * ny + nz * nz);
        if (len > 0 && Math.abs(ny) / len > 0.3) { surface.add(a); surface.add(b); surface.add(c); }
    }
    const out: number[] = [];
    for (const [cx, cz] of [[-64, -64], [-64, 64], [64, 64], [64, -64]]) {
        let best = -1, bestD = Infinity, near = -Infinity;
        for (const v of surface) {
            const dx = m.verticesX[v] - cx, dz = m.verticesZ[v] - cz;
            const d = dx * dx + dz * dz;
            if (d <= 12 * 12) near = Math.max(near, -m.verticesY[v]);
            if (d < bestD) { bestD = d; best = v; }
        }
        out.push(near > -Infinity ? near : best >= 0 ? -m.verticesY[best] : 0);
    }
    return out;
}

// a roof piece's top surface sampled every 32 units (a quarter tile) over the piece's whole reach (its slopes and
// overhangs run past its own tile): { x0, z0, nx, nz, h } with x0, z0 the first sample's place from the tile's middle
// (x east, z north) and h the heights up, row by row (z outer), -1 where the piece does not cover the point. Only faces
// that are not upright count, and the highest at each point.
function roofSurface(t: any, type: number, rot: number): any {
    let m: any;
    try { m = locModels.getLocModelData(t, type, rot); } catch (e) { return undefined; }
    if (!m || !m.verticesCount) return undefined;
    const tris: number[][] = [];
    // the surface's look: area by texture (after the loc's own swaps) or colour, the largest wins
    const areas = new Map<string, number>();
    let minX = Infinity, maxX = -Infinity, minZ = Infinity, maxZ = -Infinity;
    for (let f = 0; f < m.faceCount; f++) {
        if (m.faceAlphas && (m.faceAlphas[f] & 0xff) > 200) continue;
        const a = m.indices1[f], b = m.indices2[f], c = m.indices3[f];
        const ax = m.verticesX[a], ay = -m.verticesY[a], az = m.verticesZ[a];
        const bx = m.verticesX[b], by = -m.verticesY[b], bz = m.verticesZ[b];
        const cx = m.verticesX[c], cy = -m.verticesY[c], cz = m.verticesZ[c];
        const ux = bx - ax, uy = by - ay, uz = bz - az, vx = cx - ax, vy = cy - ay, vz = cz - az;
        const nx = uy * vz - uz * vy, ny = uz * vx - ux * vz, nz = ux * vy - uy * vx;
        const len = Math.sqrt(nx * nx + ny * ny + nz * nz);
        if (!(len > 0) || Math.abs(ny) / len < 0.3) continue;
        tris.push([ax, ay, az, bx, by, bz, cx, cy, cz]);
        let tex = m.faceTextures ? m.faceTextures[f] : -1;
        if (tex >= 0 && t.retextureFrom) { const ri = t.retextureFrom.indexOf(tex); if (ri >= 0) tex = t.retextureTo[ri]; }
        let hsl = m.faceColors[f];
        if (tex < 0 && t.recolorFrom) { const ci = t.recolorFrom.indexOf(hsl); if (ci >= 0) hsl = t.recolorTo[ci]; }
        const key = tex >= 0 ? "t" + tex : "c" + hsl;
        areas.set(key, (areas.get(key) ?? 0) + len);
        minX = Math.min(minX, ax, bx, cx); maxX = Math.max(maxX, ax, bx, cx);
        minZ = Math.min(minZ, az, bz, cz); maxZ = Math.max(maxZ, az, bz, cz);
    }
    if (!tris.length) return undefined;
    const step = 32;
    const x0 = Math.ceil(minX / step) * step, z0 = Math.ceil(minZ / step) * step;
    const nxs = Math.floor((maxX - x0) / step) + 1, nzs = Math.floor((maxZ - z0) / step) + 1;
    const h: number[] = [];
    for (let j = 0; j < nzs; j++) {
        for (let i = 0; i < nxs; i++) {
            const px = x0 + i * step, pz = z0 + j * step;
            let best = -1;
            for (const tr of tris) {
                // the point in the triangle (in x, z) and its height there
                const [ax, ay, az, bx, by, bz, cx, cy, cz] = tr;
                const d = (bz - cz) * (ax - cx) + (cx - bx) * (az - cz);
                if (d === 0) continue;
                const w1 = ((bz - cz) * (px - cx) + (cx - bx) * (pz - cz)) / d;
                const w2 = ((cz - az) * (px - cx) + (ax - cx) * (pz - cz)) / d;
                const w3 = 1 - w1 - w2;
                if (w1 < -0.01 || w2 < -0.01 || w3 < -0.01) continue;
                best = Math.max(best, w1 * ay + w2 * by + w3 * cy);
            }
            h.push(best < 0 ? -1 : Math.round(best));
        }
    }
    let bestKey = "", bestArea = -1;
    for (const [k, a] of areas) if (a > bestArea) { bestArea = a; bestKey = k; }
    let rgb = -1;
    if (bestKey.startsWith("t")) { try { rgb = HSL_RGB_MAP[textureLoader.getAverageHsl(parseInt(bestKey.slice(1))) & 0xffff]; } catch (e) {} }
    else if (bestKey) rgb = HSL_RGB_MAP[parseInt(bestKey.slice(1)) & 0xffff];
    return { x0, z0, nx: nxs, nz: nzs, h, rgb, texture: bestKey.startsWith("t") ? parseInt(bestKey.slice(1)) : -1 };
}

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
                const type = attributes >> 2;
                locs.push({
                    roof: type >= 12 && type <= 21 ? roofCorners(t, type, attributes & 3) : undefined,
                    // roof pieces, and unnamed objects on upper levels (some roofs are modelled as plain objects)
                    surface: (type >= 12 && type <= 21) || ((type === 10 || type === 11) && level > 0 && (!t.name || t.name === "null")) ? roofSurface(t, type, attributes & 3) : undefined,
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
        if ((scene.tileRenderFlags[1][x][y] & 2) === 2)
            // with the deck's own heights (level 1's, up) at the tile's south west, north west, north east, south east
            bridges.push([x, y, -scene.tileHeights[1][x][y], -scene.tileHeights[1][x][y + 1], -scene.tileHeights[1][x + 1][y + 1], -scene.tileHeights[1][x + 1][y]]);
// the upper floors (levels 1 to 3): every tile with ground drawn on it, as [x, y]
const floors: number[][][] = [[], [], [], []];
for (let level = 1; level < Scene.MAX_LEVELS; level++)
    for (let x = 0; x < sizeX; x++)
        for (let y = 0; y < sizeY; y++)
            if (scene.tileOverlays[level][x][y] > 0 || scene.tileUnderlays[level][x][y] > 0) {
                // with its colour: the overlay's (its texture's average for a textured one), else the underlay's
                let rgb = -1;
                const o = scene.tileOverlays[level][x][y];
                const u = scene.tileUnderlays[level][x][y];
                if (o > 0) {
                    const ot: any = overlays.load(o - 1);
                    if (ot.textureId >= 0) { try { rgb = HSL_RGB_MAP[textureLoader.getAverageHsl(ot.textureId) & 0xffff]; } catch (e) {} }
                    else if (ot.primaryRgb >= 0 && ot.primaryRgb !== 0xff00ff) rgb = ot.primaryRgb;
                }
                if (rgb < 0 && u > 0) rgb = (underlays.load(u - 1) as any).rgbColor;
                floors[level].push([x, y, rgb]);
            }
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
