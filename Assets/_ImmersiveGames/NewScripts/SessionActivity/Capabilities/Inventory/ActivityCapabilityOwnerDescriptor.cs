using _ImmersiveGames.NewScripts.UnityUtils;
namespace _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory
{
    public readonly struct ActivityCapabilityOwnerDescriptor
    {
        public ActivityCapabilityOwnerDescriptor(
            ActivityCapabilityOwnerKind ownerKind,
            string ownerId,
            string ownerPath,
            string sourceScene,
            string sourceContent,
            string source)
        {
            OwnerKind = ownerKind;
            OwnerId = ownerId.TrimToEmpty();
            OwnerPath = ownerPath.TrimToEmpty();
            SourceScene = sourceScene.TrimToEmpty();
            SourceContent = sourceContent.TrimToEmpty();
            Source = source.TrimToEmpty();
        }

        public ActivityCapabilityOwnerKind OwnerKind { get; }
        public string OwnerId { get; }
        public string OwnerPath { get; }
        public string SourceScene { get; }
        public string SourceContent { get; }
        public string Source { get; }

        public bool HasOwnerPath => !string.IsNullOrWhiteSpace(OwnerPath);
        public bool HasSourceScene => !string.IsNullOrWhiteSpace(SourceScene);
        public bool HasSourceContent => !string.IsNullOrWhiteSpace(SourceContent);
        public bool IsValid =>
            OwnerKind != ActivityCapabilityOwnerKind.Unknown &&
            !string.IsNullOrWhiteSpace(OwnerId);

        public override string ToString()
        {
            return $"ownerKind='{OwnerKind}', ownerId='{OwnerId}', ownerPath='{OwnerPath}', sourceScene='{SourceScene}', sourceContent='{SourceContent}'";
        }
}
}
