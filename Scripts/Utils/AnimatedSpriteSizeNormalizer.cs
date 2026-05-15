using Godot;

namespace EchoSpace.Utils;

[GlobalClass]
public partial class AnimatedSpriteSizeNormalizer : Node
{
	[Export] public NodePath? SpritePath { get; set; } = new("..");
	[Export(PropertyHint.Range, "1,2048,1")] public float TargetFrameHeight { get; set; } = 64f;
	[Export] public bool ReapplyOnReady { get; set; } = true;
	[Export] public bool ReapplyEveryFrameChange { get; set; } = true;

	private AnimatedSprite2D? _sprite;

	public override void _Ready()
	{
		_sprite = SpritePath != null && !SpritePath.IsEmpty
			? GetNodeOrNull<AnimatedSprite2D>(SpritePath)
			: null;

		if (_sprite == null)
		{
			GD.PushWarning($"{nameof(AnimatedSpriteSizeNormalizer)}: AnimatedSprite2D not found.");
			return;
		}

		if (ReapplyOnReady)
		{
			ApplyNormalization();
		}

		if (ReapplyEveryFrameChange)
		{
			_sprite.FrameChanged += OnFrameChanged;
		}
	}

	public override void _ExitTree()
	{
		if (_sprite != null)
		{
			_sprite.FrameChanged -= OnFrameChanged;
		}
	}

	private void OnFrameChanged()
	{
		ApplyNormalization();
	}

	public void ApplyNormalization()
	{
		if (_sprite == null)
		{
			return;
		}

		var frameTexture = _sprite.SpriteFrames?.GetFrameTexture(_sprite.Animation, _sprite.Frame);
		if (frameTexture == null)
		{
			return;
		}

		var sourceHeight = frameTexture.GetHeight();
		if (sourceHeight <= 0f)
		{
			return;
		}

		var uniformScale = TargetFrameHeight / sourceHeight;
		_sprite.Scale = new Vector2(uniformScale, uniformScale);
	}
}
