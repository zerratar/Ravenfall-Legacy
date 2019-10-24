using RavenNest.Models;
using System;
using UnityEngine;

public interface IPlayerAppearance
{
    Gender Gender { get; }
    void SetAppearance(Appearance appearance);
    void SetAppearance(SyntyAppearance appearance);
    bool TryUpdate(int[] values);
    int[] ToAppearanceData();
    SyntyAppearance ToSyntyAppearanceData();
    void ToggleHelmVisibility();
    void UpdateAppearance();
    void Optimize(Action afterUndo = null);
    void Equip(ItemController item);
    void UnEquip(ItemController item);

    Transform MainHandTransform { get; }
    Transform OffHandTransform { get; }
}