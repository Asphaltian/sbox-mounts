namespace Source1.Dods;

public sealed class Mount : SourceGameMount
{
	protected override string ContentDirectory => "dod";
	public override string Ident => "dods";
	public override string Title => "Day of Defeat: Source";
	public override long AppId => 300;
}
