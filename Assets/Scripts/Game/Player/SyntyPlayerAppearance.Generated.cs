using UnityEngine;
using RavenNest.Models;

// Parts get their renderers through RendererOf: players carry their unused parts without one (see PlayerParts).
// Keep it that way if this file is generated again.
public partial class SyntyPlayerAppearance
{
    /// <summary>
    ///     This method has been generated, do not modify unless necessary as any changes may be overwritten in the future.
    /// </summary>
    public void ResetAppearance()
    {
        for (var i = 0; i < hats.Length; ++i)
        {
            hats[i].SetActive(false);
        }
        for (var i = 0; i < masks.Length; ++i)
        {
            masks[i].SetActive(false);
        }
        for (var i = 0; i < headCoverings.Length; ++i)
        {
            headCoverings[i].SetActive(false);
        }
        for (var i = 0; i < capes.Length; ++i)
        {
            capes[i].SetActive(false);
        }
        for (var i = 0; i < hairs.Length; ++i)
        {
            hairs[i]?.SetActive(false);
        }
        for (var i = 0; i < headAttachments.Length; ++i)
        {
            headAttachments[i].SetActive(false);
        }
        for (var i = 0; i < ears.Length; ++i)
        {
            ears[i].SetActive(false);
        }
        for (var i = 0; i < shoulderPadsRight.Length; ++i)
        {
            shoulderPadsRight[i].SetActive(false);
        }
        for (var i = 0; i < shoulderPadsLeft.Length; ++i)
        {
            shoulderPadsLeft[i].SetActive(false);
        }
        for (var i = 0; i < elbowsRight.Length; ++i)
        {
            elbowsRight[i].SetActive(false);
        }
        for (var i = 0; i < elbowsLeft.Length; ++i)
        {
            elbowsLeft[i].SetActive(false);
        }
        for (var i = 0; i < kneeAttachmentsRight.Length; ++i)
        {
            kneeAttachmentsRight[i].SetActive(false);
        }
        for (var i = 0; i < kneeAttachmentsLeft.Length; ++i)
        {
            kneeAttachmentsLeft[i].SetActive(false);
        }
        for (var i = 0; i < maleHeads.Length; ++i)
        {
            maleHeads[i].SetActive(false);
        }
        for (var i = 0; i < maleHelmets.Length; ++i)
        {
            maleHelmets[i].SetActive(false);
        }
        for (var i = 0; i < maleEyebrows.Length; ++i)
        {
            maleEyebrows[i].SetActive(false);
        }
        for (var i = 0; i < maleFacialHairs.Length; ++i)
        {
            maleFacialHairs[i].SetActive(false);
        }
        for (var i = 0; i < maleTorso.Length; ++i)
        {
            maleTorso[i].SetActive(false);
        }
        for (var i = 0; i < maleArmUpperRight.Length; ++i)
        {
            maleArmUpperRight[i].SetActive(false);
        }
        for (var i = 0; i < maleArmUpperLeft.Length; ++i)
        {
            maleArmUpperLeft[i].SetActive(false);
        }
        for (var i = 0; i < maleArmLowerRight.Length; ++i)
        {
            maleArmLowerRight[i].SetActive(false);
        }
        for (var i = 0; i < maleArmLowerLeft.Length; ++i)
        {
            maleArmLowerLeft[i].SetActive(false);
        }
        for (var i = 0; i < maleHandsRight.Length; ++i)
        {
            maleHandsRight[i].SetActive(false);
        }
        for (var i = 0; i < maleHandsLeft.Length; ++i)
        {
            maleHandsLeft[i].SetActive(false);
        }
        for (var i = 0; i < maleHips.Length; ++i)
        {
            maleHips[i].SetActive(false);
        }
        for (var i = 0; i < maleLegsRight.Length; ++i)
        {
            maleLegsRight[i].SetActive(false);
        }
        for (var i = 0; i < maleLegsLeft.Length; ++i)
        {
            maleLegsLeft[i].SetActive(false);
        }
        for (var i = 0; i < femaleHeads.Length; ++i)
        {
            femaleHeads[i].SetActive(false);
        }
        for (var i = 0; i < femaleHelmets.Length; ++i)
        {
            femaleHelmets[i].SetActive(false);
        }
        for (var i = 0; i < femaleEyebrows.Length; ++i)
        {
            femaleEyebrows[i].SetActive(false);
        }
        for (var i = 0; i < femaleFacialHairs.Length; ++i)
        {
            femaleFacialHairs[i].SetActive(false);
        }
        for (var i = 0; i < femaleTorso.Length; ++i)
        {
            femaleTorso[i].SetActive(false);
        }
        for (var i = 0; i < femaleArmUpperRight.Length; ++i)
        {
            femaleArmUpperRight[i].SetActive(false);
        }
        for (var i = 0; i < femaleArmUpperLeft.Length; ++i)
        {
            femaleArmUpperLeft[i].SetActive(false);
        }
        for (var i = 0; i < femaleArmLowerRight.Length; ++i)
        {
            femaleArmLowerRight[i].SetActive(false);
        }
        for (var i = 0; i < femaleArmLowerLeft.Length; ++i)
        {
            femaleArmLowerLeft[i].SetActive(false);
        }
        for (var i = 0; i < femaleHandsRight.Length; ++i)
        {
            femaleHandsRight[i].SetActive(false);
        }
        for (var i = 0; i < femaleHandsLeft.Length; ++i)
        {
            femaleHandsLeft[i].SetActive(false);
        }
        for (var i = 0; i < femaleHips.Length; ++i)
        {
            femaleHips[i].SetActive(false);
        }
        for (var i = 0; i < femaleLegsRight.Length; ++i)
        {
            femaleLegsRight[i].SetActive(false);
        }
        for (var i = 0; i < femaleLegsLeft.Length; ++i)
        {
            femaleLegsLeft[i].SetActive(false);
        }
        if (useMeshCombiner && (meshCombiner?.isMeshesCombineds ?? false))
        {
            //Search by gameobject containing merged meshes
            Transform objMerged = GetComponent<Transform>().Find("Combined Mesh");
            //Destroy the object, and clear memory
            if (objMerged != null)
            {
                // the meshes the combiner made are not freed with their object
                var merged = CombinedMeshesOf(objMerged);
                DestroyImmediate(objMerged.gameObject, true);
                foreach (var mesh in merged) if (mesh) Destroy(mesh);
            }

            meshCombiner?.Clear();
        }
    }

    /// <summary>
    ///     This method has been generated, do not modify unless necessary as any changes may be overwritten in the future.
    /// </summary>
    private void LoadAppearance(SyntyAppearance appearance)
    {
        Gender = appearance.Gender;
        Hair = appearance.Hair;
        Head = appearance.Head;
        Eyebrows = appearance.Eyebrows;
        FacialHair = appearance.FacialHair;
        Cape = appearance.Cape;
        SkinColor = GetColorFromHex(appearance.SkinColor);
        HairColor = GetColorFromHex(appearance.HairColor);
        BeardColor = GetColorFromHex(appearance.BeardColor);
        EyeColor = GetColorFromHex(appearance.EyeColor);
        HelmetVisible = appearance.HelmetVisible;
        StubbleColor = GetColorFromHex(appearance.StubbleColor);
        WarPaintColor = GetColorFromHex(appearance.WarPaintColor);
    }


    /// <summary>
    ///     This method has been generated, do not modify unless necessary as any changes may be overwritten in the future.
    /// </summary>
    private void InitAppearance()
    {
        capeLogoMaterials.Clear();
        var i_capes = Cape;
        if (i_capes >= 0 && i_capes < capes.Length)
        {
            var m_capes = capes[i_capes];
            var r_capes = RendererOf(m_capes);
            m_capes.SetActive(true);
            capeLogoMaterials.Add(r_capes.material);
        }

        var i_hairs = Hair;
        if (i_hairs >= 0 && i_hairs < hairs.Length)
        {
            var m_hairs = hairs[i_hairs];
            if (m_hairs)
            {
                m_hairs.SetActive(true);
            }

            var r_hairs = RendererOf(m_hairs);
            if (r_hairs)
            {
                r_hairs.material.SetColor("_Color_Hair", BeardColor);
            }
        }
        for (var i = 0; i < HeadAttachments.Length; ++i)
        {
            var itemIndex = HeadAttachments[i];
            if (itemIndex >= 0)
            {
                headAttachments[itemIndex].SetActive(true);
            }
        }
        // ears does not have an appearance field mapped to it, skipped.
        var i_shoulderPadsRight = Shoulder;
        if (i_shoulderPadsRight >= 0 && i_shoulderPadsRight < shoulderPadsRight.Length)
        {
            var m_shoulderPadsRight = shoulderPadsRight[i_shoulderPadsRight];
            var r_shoulderPadsRight = RendererOf(m_shoulderPadsRight);
            m_shoulderPadsRight.SetActive(true);
            if (i_shoulderPadsRight == 0)
            {
                r_shoulderPadsRight.material.SetColor("_Color_Skin", SkinColor);
            }
        }
        var i_shoulderPadsLeft = Shoulder;
        if (i_shoulderPadsLeft >= 0 && i_shoulderPadsLeft < shoulderPadsLeft.Length)
        {
            var m_shoulderPadsLeft = shoulderPadsLeft[i_shoulderPadsLeft];
            var r_shoulderPadsLeft = RendererOf(m_shoulderPadsLeft);
            m_shoulderPadsLeft.SetActive(true);
            if (i_shoulderPadsLeft == 0)
            {
                r_shoulderPadsLeft.material.SetColor("_Color_Skin", SkinColor);
            }
        }
        var i_elbowsRight = Elbow;
        if (i_elbowsRight >= 0 && i_elbowsRight < elbowsRight.Length)
        {
            var m_elbowsRight = elbowsRight[i_elbowsRight];
            var r_elbowsRight = RendererOf(m_elbowsRight);
            m_elbowsRight.SetActive(true);
            if (i_elbowsRight == 0)
            {
                r_elbowsRight.material.SetColor("_Color_Skin", SkinColor);
            }
        }
        var i_elbowsLeft = Elbow;
        if (i_elbowsLeft >= 0 && i_elbowsLeft < elbowsLeft.Length)
        {
            var m_elbowsLeft = elbowsLeft[i_elbowsLeft];
            var r_elbowsLeft = RendererOf(m_elbowsLeft);
            m_elbowsLeft.SetActive(true);
            if (i_elbowsLeft == 0)
            {
                r_elbowsLeft.material.SetColor("_Color_Skin", SkinColor);
            }
        }
        // kneeAttachmentsRight does not have an appearance field mapped to it, skipped.
        // kneeAttachmentsLeft does not have an appearance field mapped to it, skipped.
        if (Gender == Gender.Male)
        {
            var i_maleHeads = Head;
            if (i_maleHeads >= 0 && i_maleHeads < maleHeads.Length)
            {
                var m_maleHeads = maleHeads[i_maleHeads];
                var r_maleHeads = RendererOf(m_maleHeads);
                m_maleHeads.SetActive(true);
                r_maleHeads.material.SetColor("_Color_Eyes", EyeColor);
                r_maleHeads.material.SetColor("_Color_Skin", SkinColor);
                r_maleHeads.material.SetColor("_Color_Stubble", StubbleColor);
                r_maleHeads.material.SetColor("_Color_BodyArt", WarPaintColor);
            }
            var i_maleHelmets = Helmet;
            if (i_maleHelmets >= 0 && i_maleHelmets < maleHelmets.Length)
            {
                var m_maleHelmets = maleHelmets[i_maleHelmets];
                var r_maleHelmets = RendererOf(m_maleHelmets);
                m_maleHelmets.SetActive(true);
                if (i_maleHelmets == 0)
                {
                    r_maleHelmets.material.SetColor("_Color_Skin", SkinColor);
                }
            }
            var i_maleEyebrows = Eyebrows;
            if (i_maleEyebrows >= 0 && i_maleEyebrows < maleEyebrows.Length)
            {
                var m_maleEyebrows = maleEyebrows[i_maleEyebrows];
                var r_maleEyebrows = RendererOf(m_maleEyebrows);
                m_maleEyebrows.SetActive(true);
                r_maleEyebrows.material.SetColor("_Color_Hair", BeardColor);
            }
            var i_maleFacialHairs = FacialHair;
            if (i_maleFacialHairs >= 0 && i_maleFacialHairs < maleFacialHairs.Length)
            {
                var m_maleFacialHairs = maleFacialHairs[i_maleFacialHairs];
                var r_maleFacialHairs = RendererOf(m_maleFacialHairs);
                m_maleFacialHairs.SetActive(true);
                r_maleFacialHairs.material.SetColor("_Color_Hair", BeardColor);
            }
            var i_maleTorso = Torso;
            if (i_maleTorso >= 0 && i_maleTorso < maleTorso.Length)
            {
                var m_maleTorso = maleTorso[i_maleTorso];
                var r_maleTorso = RendererOf(m_maleTorso);
                m_maleTorso.SetActive(true);
                if (i_maleTorso == 0)
                {
                    r_maleTorso.material.SetColor("_Color_Skin", SkinColor);
                }
            }
            var i_maleArmUpperRight = ArmUpper;
            if (i_maleArmUpperRight >= 0 && i_maleArmUpperRight < maleArmUpperRight.Length)
            {
                var m_maleArmUpperRight = maleArmUpperRight[i_maleArmUpperRight];
                var r_maleArmUpperRight = RendererOf(m_maleArmUpperRight);
                m_maleArmUpperRight.SetActive(true);
                if (i_maleArmUpperRight == 0)
                {
                    r_maleArmUpperRight.material.SetColor("_Color_Skin", SkinColor);
                }
            }
            var i_maleArmUpperLeft = ArmUpper;
            if (i_maleArmUpperLeft >= 0 && i_maleArmUpperLeft < maleArmUpperLeft.Length)
            {
                var m_maleArmUpperLeft = maleArmUpperLeft[i_maleArmUpperLeft];
                var r_maleArmUpperLeft = RendererOf(m_maleArmUpperLeft);
                m_maleArmUpperLeft.SetActive(true);
                if (i_maleArmUpperLeft == 0)
                {
                    r_maleArmUpperLeft.material.SetColor("_Color_Skin", SkinColor);
                }
            }
            var i_maleArmLowerRight = ArmLower;
            if (i_maleArmLowerRight >= 0 && i_maleArmLowerRight < maleArmLowerRight.Length)
            {
                var m_maleArmLowerRight = maleArmLowerRight[i_maleArmLowerRight];
                var r_maleArmLowerRight = RendererOf(m_maleArmLowerRight);
                m_maleArmLowerRight.SetActive(true);
                if (i_maleArmLowerRight == 0)
                {
                    r_maleArmLowerRight.material.SetColor("_Color_Skin", SkinColor);
                }
            }
            var i_maleArmLowerLeft = ArmLower;
            if (i_maleArmLowerLeft >= 0 && i_maleArmLowerLeft < maleArmLowerLeft.Length)
            {
                var m_maleArmLowerLeft = maleArmLowerLeft[i_maleArmLowerLeft];
                var r_maleArmLowerLeft = RendererOf(m_maleArmLowerLeft);
                m_maleArmLowerLeft.SetActive(true);
                if (i_maleArmLowerLeft == 0)
                {
                    r_maleArmLowerLeft.material.SetColor("_Color_Skin", SkinColor);
                }
            }
            var i_maleHandsRight = Hands;
            if (i_maleHandsRight >= 0 && i_maleHandsRight < maleHandsRight.Length)
            {
                var m_maleHandsRight = maleHandsRight[i_maleHandsRight];
                var r_maleHandsRight = RendererOf(m_maleHandsRight);
                m_maleHandsRight.SetActive(true);
                if (i_maleHandsRight == 0)
                {
                    r_maleHandsRight.material.SetColor("_Color_Skin", SkinColor);
                }
            }
            var i_maleHandsLeft = Hands;
            if (i_maleHandsLeft >= 0 && i_maleHandsLeft < maleHandsLeft.Length)
            {
                var m_maleHandsLeft = maleHandsLeft[i_maleHandsLeft];
                var r_maleHandsLeft = RendererOf(m_maleHandsLeft);
                m_maleHandsLeft.SetActive(true);
                if (i_maleHandsLeft == 0)
                {
                    r_maleHandsLeft.material.SetColor("_Color_Skin", SkinColor);
                }
            }
            var i_maleHips = Hips;
            if (i_maleHips >= 0 && i_maleHips < maleHips.Length)
            {
                var m_maleHips = maleHips[i_maleHips];
                var r_maleHips = RendererOf(m_maleHips);
                m_maleHips.SetActive(true);
                if (i_maleHips == 0)
                {
                    r_maleHips.material.SetColor("_Color_Skin", SkinColor);
                }
            }
            var i_maleLegsRight = Legs;
            if (i_maleLegsRight >= 0 && i_maleLegsRight < maleLegsRight.Length)
            {
                var m_maleLegsRight = maleLegsRight[i_maleLegsRight];
                var r_maleLegsRight = RendererOf(m_maleLegsRight);
                m_maleLegsRight.SetActive(true);
                if (i_maleLegsRight == 0)
                {
                    r_maleLegsRight.material.SetColor("_Color_Skin", SkinColor);
                }
            }
            var i_maleLegsLeft = Legs;
            if (i_maleLegsLeft >= 0 && i_maleLegsLeft < maleLegsLeft.Length)
            {
                var m_maleLegsLeft = maleLegsLeft[i_maleLegsLeft];
                var r_maleLegsLeft = RendererOf(m_maleLegsLeft);
                m_maleLegsLeft.SetActive(true);
                if (i_maleLegsLeft == 0)
                {
                    r_maleLegsLeft.material.SetColor("_Color_Skin", SkinColor);
                }
            }
        }
        if (Gender == Gender.Female)
        {
            var i_femaleHeads = Head;
            if (i_femaleHeads >= 0 && i_femaleHeads < femaleHeads.Length)
            {
                var m_femaleHeads = femaleHeads[i_femaleHeads];
                var r_femaleHeads = RendererOf(m_femaleHeads);
                m_femaleHeads.SetActive(true);
                r_femaleHeads.material.SetColor("_Color_Eyes", EyeColor);
                r_femaleHeads.material.SetColor("_Color_Skin", SkinColor);
                r_femaleHeads.material.SetColor("_Color_Stubble", StubbleColor);
                r_femaleHeads.material.SetColor("_Color_BodyArt", WarPaintColor);
            }
            var i_femaleHelmets = Helmet;
            if (i_femaleHelmets >= 0 && i_femaleHelmets < femaleHelmets.Length)
            {
                var m_femaleHelmets = femaleHelmets[i_femaleHelmets];
                var r_femaleHelmets = RendererOf(m_femaleHelmets);
                m_femaleHelmets.SetActive(true);
                if (i_femaleHelmets == 0)
                {
                    r_femaleHelmets.material.SetColor("_Color_Skin", SkinColor);
                }
            }
            var i_femaleEyebrows = Eyebrows;
            if (i_femaleEyebrows >= 0 && i_femaleEyebrows < femaleEyebrows.Length)
            {
                var m_femaleEyebrows = femaleEyebrows[i_femaleEyebrows];
                var r_femaleEyebrows = RendererOf(m_femaleEyebrows);
                m_femaleEyebrows.SetActive(true);
                r_femaleEyebrows.material.SetColor("_Color_Hair", BeardColor);
            }
            var i_femaleFacialHairs = FacialHair;
            if (i_femaleFacialHairs >= 0 && i_femaleFacialHairs < femaleFacialHairs.Length)
            {
                var m_femaleFacialHairs = femaleFacialHairs[i_femaleFacialHairs];
                var r_femaleFacialHairs = RendererOf(m_femaleFacialHairs);
                m_femaleFacialHairs.SetActive(true);
                if (i_femaleFacialHairs == 0)
                {
                    r_femaleFacialHairs.material.SetColor("_Color_Skin", SkinColor);
                }
            }
            var i_femaleTorso = Torso;
            if (i_femaleTorso >= 0 && i_femaleTorso < femaleTorso.Length)
            {
                var m_femaleTorso = femaleTorso[i_femaleTorso];
                var r_femaleTorso = RendererOf(m_femaleTorso);
                m_femaleTorso.SetActive(true);
                if (i_femaleTorso == 0)
                {
                    r_femaleTorso.material.SetColor("_Color_Skin", SkinColor);
                }
            }
            var i_femaleArmUpperRight = ArmUpper;
            if (i_femaleArmUpperRight >= 0 && i_femaleArmUpperRight < femaleArmUpperRight.Length)
            {
                var m_femaleArmUpperRight = femaleArmUpperRight[i_femaleArmUpperRight];
                var r_femaleArmUpperRight = RendererOf(m_femaleArmUpperRight);
                m_femaleArmUpperRight.SetActive(true);
                if (i_femaleArmUpperRight == 0)
                {
                    r_femaleArmUpperRight.material.SetColor("_Color_Skin", SkinColor);
                }
            }
            var i_femaleArmUpperLeft = ArmUpper;
            if (i_femaleArmUpperLeft >= 0 && i_femaleArmUpperLeft < femaleArmUpperLeft.Length)
            {
                var m_femaleArmUpperLeft = femaleArmUpperLeft[i_femaleArmUpperLeft];
                var r_femaleArmUpperLeft = RendererOf(m_femaleArmUpperLeft);
                m_femaleArmUpperLeft.SetActive(true);
                if (i_femaleArmUpperLeft == 0)
                {
                    r_femaleArmUpperLeft.material.SetColor("_Color_Skin", SkinColor);
                }
            }
            var i_femaleArmLowerRight = ArmLower;
            if (i_femaleArmLowerRight >= 0 && i_femaleArmLowerRight < femaleArmLowerRight.Length)
            {
                var m_femaleArmLowerRight = femaleArmLowerRight[i_femaleArmLowerRight];
                var r_femaleArmLowerRight = RendererOf(m_femaleArmLowerRight);
                m_femaleArmLowerRight.SetActive(true);
                if (i_femaleArmLowerRight == 0)
                {
                    r_femaleArmLowerRight.material.SetColor("_Color_Skin", SkinColor);
                }
            }
            var i_femaleArmLowerLeft = ArmLower;
            if (i_femaleArmLowerLeft >= 0 && i_femaleArmLowerLeft < femaleArmLowerLeft.Length)
            {
                var m_femaleArmLowerLeft = femaleArmLowerLeft[i_femaleArmLowerLeft];
                var r_femaleArmLowerLeft = RendererOf(m_femaleArmLowerLeft);
                m_femaleArmLowerLeft.SetActive(true);
                if (i_femaleArmLowerLeft == 0)
                {
                    r_femaleArmLowerLeft.material.SetColor("_Color_Skin", SkinColor);
                }
            }
            var i_femaleHandsRight = Hands;
            if (i_femaleHandsRight >= 0 && i_femaleHandsRight < femaleHandsRight.Length)
            {
                var m_femaleHandsRight = femaleHandsRight[i_femaleHandsRight];
                var r_femaleHandsRight = RendererOf(m_femaleHandsRight);
                m_femaleHandsRight.SetActive(true);
                if (i_femaleHandsRight == 0)
                {
                    r_femaleHandsRight.material.SetColor("_Color_Skin", SkinColor);
                }
            }
            var i_femaleHandsLeft = Hands;
            if (i_femaleHandsLeft >= 0 && i_femaleHandsLeft < femaleHandsLeft.Length)
            {
                var m_femaleHandsLeft = femaleHandsLeft[i_femaleHandsLeft];
                var r_femaleHandsLeft = RendererOf(m_femaleHandsLeft);
                m_femaleHandsLeft.SetActive(true);
                if (i_femaleHandsLeft == 0)
                {
                    r_femaleHandsLeft.material.SetColor("_Color_Skin", SkinColor);
                }
            }
            var i_femaleHips = Hips;
            if (i_femaleHips >= 0 && i_femaleHips < femaleHips.Length)
            {
                var m_femaleHips = femaleHips[i_femaleHips];
                var r_femaleHips = RendererOf(m_femaleHips);
                m_femaleHips.SetActive(true);
                if (i_femaleHips == 0)
                {
                    r_femaleHips.material.SetColor("_Color_Skin", SkinColor);
                }
            }
            var i_femaleLegsRight = Legs;
            if (i_femaleLegsRight >= 0 && i_femaleLegsRight < femaleLegsRight.Length)
            {
                var m_femaleLegsRight = femaleLegsRight[i_femaleLegsRight];
                var r_femaleLegsRight = RendererOf(m_femaleLegsRight);
                m_femaleLegsRight.SetActive(true);
                if (i_femaleLegsRight == 0)
                {
                    r_femaleLegsRight.material.SetColor("_Color_Skin", SkinColor);
                }
            }
            var i_femaleLegsLeft = Legs;
            if (i_femaleLegsLeft >= 0 && i_femaleLegsLeft < femaleLegsLeft.Length)
            {
                var m_femaleLegsLeft = femaleLegsLeft[i_femaleLegsLeft];
                var r_femaleLegsLeft = RendererOf(m_femaleLegsLeft);
                m_femaleLegsLeft.SetActive(true);
                if (i_femaleLegsLeft == 0)
                {
                    r_femaleLegsLeft.material.SetColor("_Color_Skin", SkinColor);
                }
            }
        }
    }

}