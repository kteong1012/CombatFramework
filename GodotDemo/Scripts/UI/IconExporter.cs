using Godot;
using System;

/// <summary>
/// 图标导出工具 — 运行此场景将 SkillIcon 绘制生成为 PNG 文件。
/// 只运行一次，导出到 res://Textures/ 目录后即可不再使用。
/// </summary>
public partial class IconExporter : Node2D
{
    public override void _Ready()
    {
        Export(SkillIcon.IconType.Slash,  "icon_slash.png");
        Export(SkillIcon.IconType.Burst,  "icon_burst.png");
        Export(SkillIcon.IconType.Charge, "icon_charge.png");

        GD.Print("三张图标已导出到 Textures/ 目录");
        GetTree().Quit();
    }

    private void Export(SkillIcon.IconType type, string filename)
    {
        const int size = 128;

        // 1. 创建离屏 Viewport 和 SkillIcon 节点
        var viewport = new SubViewport();
        viewport.Size = new Vector2I(size, size);
        viewport.TransparentBg = true;
        viewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Once;
        AddChild(viewport);

        var icon = new SkillIcon
        {
            Type = type,
            Size = new Vector2(size, size),
        };
        viewport.AddChild(icon);

        // 2. 等待渲染帧
        RenderingServer.GlobalShaderParameterGet("time"); // dummy to flush
        viewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Once;

        // 3. 轮询直到纹理就绪
        var start = Time.GetTicksMsec();
        while (Time.GetTicksMsec() - start < 1000)
        {
            // 等待 VSync
        }

        // 4. 获取纹理数据
        var image = viewport.GetTexture().GetImage();
        if (image == null)
        {
            GD.PrintErr($"无法获取 {filename} 的纹理数据");
            return;
        }

        // 5. 保存 PNG
        var path = $"res://Textures/{filename}";
        var globalPath = ProjectSettings.GlobalizePath(path);
        image.SavePng(globalPath);
        GD.Print($"已导出: {path}");

        viewport.QueueFree();
    }
}
