namespace Source1;

partial class MapLoader
{
	private BspProbeTextures _probeTextures;

	private void BuildLightProbes( BspFile bsp, GameObject world, bool skybox = false )
	{
		_probeTextures = new BspProbeTextures( ResourcePrefix );
		new MapLightingBuilder( bsp, this, _lighting ).BuildLightProbes( world, skybox, _probeTextures );
	}
}
