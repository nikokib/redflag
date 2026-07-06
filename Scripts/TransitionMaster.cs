using Godot;
using System.Threading.Tasks;

public partial class TransitionManager : Node2D
{
	private AnimationPlayer animationPlayer;

	public override void _Ready()
	{
		animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
	}

	public async Task FadeToScene(string scenePath)
	{
		animationPlayer.Play("FadeOut");

		await ToSignal(animationPlayer, AnimationPlayer.SignalName.AnimationFinished);

		GetTree().ChangeSceneToFile(scenePath);

		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

		animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");

		animationPlayer.Play("FadeIn");
	}
}
