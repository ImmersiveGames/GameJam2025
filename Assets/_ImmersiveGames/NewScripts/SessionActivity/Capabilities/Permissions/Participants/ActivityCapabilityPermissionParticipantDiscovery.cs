using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Discovery deterministico de bindings locais de ActivityCapabilityPermission.
/// O discovery apenas encontra e ordena declaracoes locais; ele nao registra receivers, nao publica permissions
/// e nao decide lifecycle macro.
/// </summary>
public static class ActivityCapabilityPermissionParticipantDiscovery
{
    public static ActivityCapabilityPermissionParticipantDiscoveryResult Discover(
        IEnumerable<GameObject> roots,
        string ownerId,
        string permissionFilter = null)
    {
        var descriptors = new List<ActivityCapabilityPermissionParticipantDescriptor>();
        var warnings = new List<string>();

        if (roots == null)
        {
            warnings.Add("permission_binding_discovery_roots_null");
            return new ActivityCapabilityPermissionParticipantDiscoveryResult(descriptors, warnings);
        }

        foreach (var root in roots)
        {
            if (root == null)
            {
                warnings.Add("permission_binding_discovery_root_null");
                continue;
            }

            DiscoverFromRoot(root, ownerId, permissionFilter, descriptors, warnings);
        }

        descriptors.Sort(CompareDescriptors);
        return new ActivityCapabilityPermissionParticipantDiscoveryResult(descriptors, warnings);
    }

    public static ActivityCapabilityPermissionParticipantDiscoveryResult Discover(
        GameObject root,
        string ownerId,
        string permissionFilter = null)
    {
        return Discover(new[] { root }, ownerId, permissionFilter);
    }

    private static void DiscoverFromRoot(
        GameObject root,
        string ownerId,
        string permissionFilter,
        List<ActivityCapabilityPermissionParticipantDescriptor> descriptors,
        List<string> warnings)
    {
        ActivityCapabilityPermissionBindings[] bindingComponents = root.GetComponentsInChildren<ActivityCapabilityPermissionBindings>(true);
        for (int componentIndex = 0; componentIndex < bindingComponents.Length; componentIndex++)
        {
            ActivityCapabilityPermissionBindings bindingComponent = bindingComponents[componentIndex];
            if (bindingComponent == null)
            {
                continue;
            }

            string ownerPath = GetHierarchyPath(root.transform);
            string componentPath = GetHierarchyPath(bindingComponent.transform);
            ActivityCapabilityPermissionBindingEntry[] entries = bindingComponent.Bindings;
            for (int bindingIndex = 0; bindingIndex < entries.Length; bindingIndex++)
            {
                ActivityCapabilityPermissionBindingEntry entry = entries[bindingIndex];
                if (entry == null)
                {
                    warnings.Add($"permission_binding_entry_null component='{componentPath}' bindingIndex='{bindingIndex}'");
                    continue;
                }

                string permissionId = Normalize(entry.PermissionId);
                if (string.IsNullOrWhiteSpace(permissionId))
                {
                    warnings.Add($"permission_binding_missing_permission component='{componentPath}' bindingIndex='{bindingIndex}'");
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(permissionFilter) &&
                    !string.Equals(permissionId, Normalize(permissionFilter), StringComparison.Ordinal))
                {
                    continue;
                }

                MonoBehaviour[] targets = entry.ReactionTargets;
                if (targets == null || targets.Length == 0)
                {
                    warnings.Add($"permission_binding_missing_targets component='{componentPath}' bindingIndex='{bindingIndex}' permissionId='{permissionId}'");
                    continue;
                }

                for (int targetIndex = 0; targetIndex < targets.Length; targetIndex++)
                {
                    MonoBehaviour target = targets[targetIndex];
                    if (target == null)
                    {
                        warnings.Add($"permission_binding_target_null component='{componentPath}' bindingIndex='{bindingIndex}' targetIndex='{targetIndex}' permissionId='{permissionId}'");
                        continue;
                    }

                    if (target is not IActivityCapabilityPermissionReactionTarget)
                    {
                        warnings.Add($"permission_binding_target_invalid component='{componentPath}' bindingIndex='{bindingIndex}' targetIndex='{targetIndex}' permissionId='{permissionId}' targetType='{target.GetType().Name}'");
                        continue;
                    }

                    string participantId = BuildParticipantId(entry, bindingComponent, target, bindingIndex, targetIndex, permissionId);
                    if (string.IsNullOrWhiteSpace(participantId))
                    {
                        warnings.Add($"permission_binding_missing_participant_id component='{componentPath}' bindingIndex='{bindingIndex}' targetIndex='{targetIndex}' permissionId='{permissionId}'");
                        continue;
                    }

                    string targetPath = GetHierarchyPath(target.transform);
                    string orderKey = BuildOrderKey(ownerId, ownerPath, permissionId, participantId, targetPath, target.GetType().FullName);

                    descriptors.Add(new ActivityCapabilityPermissionParticipantDescriptor(
                        bindingComponent,
                        target,
                        ownerId,
                        ownerPath,
                        participantId,
                        permissionId,
                        entry.Required,
                        bindingIndex,
                        targetIndex,
                        orderKey));
                }
            }
        }
    }

    private static string BuildParticipantId(
        ActivityCapabilityPermissionBindingEntry entry,
        ActivityCapabilityPermissionBindings bindingsComponent,
        MonoBehaviour target,
        int bindingIndex,
        int targetIndex,
        string permissionId)
    {
        string seed = Normalize(entry.ParticipantIdSeed);
        if (!string.IsNullOrWhiteSpace(seed))
        {
            return $"{seed}|{Normalize(permissionId)}|b{bindingIndex}|t{targetIndex}";
        }

        string componentType = bindingsComponent.GetType().Name;
        string targetType = target.GetType().Name;
        string targetPath = GetHierarchyPath(target.transform);
        return $"{componentType}|{Normalize(permissionId)}|{targetType}|{targetPath}|b{bindingIndex}|t{targetIndex}";
    }

    private static int CompareDescriptors(
        ActivityCapabilityPermissionParticipantDescriptor left,
        ActivityCapabilityPermissionParticipantDescriptor right)
    {
        return string.CompareOrdinal(left.OrderKey, right.OrderKey);
    }

    private static string BuildOrderKey(
        string ownerId,
        string ownerPath,
        string permissionId,
        string participantId,
        string targetPath,
        string targetType)
    {
        return string.Join("|",
            Normalize(ownerId),
            Normalize(ownerPath),
            Normalize(permissionId),
            Normalize(participantId),
            Normalize(targetPath),
            Normalize(targetType));
    }

    private static string GetHierarchyPath(Transform transform)
    {
        if (transform == null)
        {
            return string.Empty;
        }

        var names = new Stack<string>();
        var current = transform;
        while (current != null)
        {
            names.Push(current.name);
            current = current.parent;
        }

        return string.Join("/", names);
    }

    private static string Normalize(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }
}
