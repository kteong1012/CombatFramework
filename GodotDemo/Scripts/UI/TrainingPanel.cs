using Godot;
using System;

/// <summary>
/// 训练场控制面板 — 能量锁定、敌人无敌、重置。
/// </summary>
public partial class TrainingPanel : Control
{
    public event Action OnResetEnemy;
    public event Action OnResetStats;

    private Button _btnEnergyLock;
    private Button _btnInvincible;
    private Button _btnResetEnemy;
    private Button _btnResetStats;

    private bool _energyLock;
    private bool _invincible;

    public override void _Ready()
    {
        _btnEnergyLock = GetNode<Button>("EnergyLock");
        _btnInvincible = GetNode<Button>("Invincible");
        _btnResetEnemy = GetNode<Button>("ResetEnemy");
        _btnResetStats  = GetNode<Button>("ResetStats");

        _btnEnergyLock.Pressed += () => ToggleEnergyLock();
        _btnInvincible.Pressed += () => ToggleInvincible();
        _btnResetEnemy.Pressed += () => OnResetEnemy?.Invoke();
        _btnResetStats.Pressed  += () => OnResetStats?.Invoke();

        UpdateButtonStyles();
    }

    private void ToggleEnergyLock()
    {
        _energyLock = !_energyLock;
        TrainingConfig.EnergyLock = _energyLock;
        UpdateButtonStyles();
    }

    private void ToggleInvincible()
    {
        _invincible = !_invincible;
        TrainingConfig.EnemyInvincible = _invincible;
        UpdateButtonStyles();
    }

    private void UpdateButtonStyles()
    {
        SetBtnStyle(_btnEnergyLock, _energyLock, "能量: ∞", "能量: 正常");
        SetBtnStyle(_btnInvincible, _invincible, "敌人: 无敌", "敌人: 正常");
    }

    private static void SetBtnStyle(Button btn, bool active, string activeText, string normalText)
    {
        btn.Text = active ? activeText : normalText;
        var style = new StyleBoxFlat();
        style.BgColor = active
            ? new Color(0.85f, 0.55f, 0.2f)
            : new Color(0.25f, 0.25f, 0.35f);
        btn.AddThemeStyleboxOverride("normal", style);
    }
}
