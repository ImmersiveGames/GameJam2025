using System;

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
            OwnerId = Normalize(ownerId);
            OwnerPath = Normalize(ownerPath);
            SourceScene = Normalize(sourceScene);
            SourceContent = Normalize(sourceContent);
            Source = Normalize(source);
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

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
