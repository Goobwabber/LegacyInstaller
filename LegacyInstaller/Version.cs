namespace LegacyInstaller
{
    public class Version
    {
        public string BSVersion;
        public string ManifestId;
        public string ReleaseURL;

        public override string ToString()
        {
            return BSVersion;
        }
    }
}
