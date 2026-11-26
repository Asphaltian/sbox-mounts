namespace Source1.Tf2;

public sealed class Mount : SourceGameMount
{
	protected override string ContentDirectory => "tf";
	public override string Ident => "tf2";
	public override string Title => "Team Fortress 2";
	public override long AppId => 440;
}
