using System;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SessionOperational.Contracts;
using _ImmersiveGames.NewScripts.SessionOperational.Pipeline;
namespace _ImmersiveGames.NewScripts.Foundation.Platform.RuntimeMode
{
    public static class SessionOperationalRuntimeConfigResolver
    {
        private static bool _loadingConfigSourceLogged;
        private static bool _startupRouteSourceLogged;

        public static SessionOperationalRuntimeLoadingDefaults ResolveLoadingDefaultsOrFail(RuntimeModeConfig runtimeModeConfig)
        {
            if (runtimeModeConfig == null)
            {
                throw new InvalidOperationException("[FATAL][Config][SessionOperationalRuntime] RuntimeModeConfig obrigatorio ausente para resolver loading defaults.");
            }

            if (RuntimeConfigRegistry.TryGetSnapshot(out IRuntimeConfigSnapshotReadOnly snapshot) && snapshot != null)
            {
                ISessionOperationalRuntimeConfigGroupReadOnly group = snapshot.SessionOperationalRuntime;
                if (group == null)
                {
                    throw new InvalidOperationException("[FATAL][Config][SessionOperationalRuntime] RuntimeConfigRegistry invariant breach: snapshot.SessionOperationalRuntime obrigatorio ausente.");
                }

                SessionOperationalRouteLoadingMode mode = group.DefaultLoadingMode;
                RuntimeLoadingProfileAsset profile = group.DefaultLoadingProfile;
                if (!TryValidate(mode, profile, out string validationError))
                {
                    throw new InvalidOperationException($"[FATAL][Config][SessionOperationalRuntime] RuntimeConfigRegistry invariant breach: loading config invalida no snapshot. detail='{validationError}'.");
                }

                LogLoadingSourceOnce(mode, profile);
                return new SessionOperationalRuntimeLoadingDefaults(mode, profile);
            }

            throw new InvalidOperationException("[FATAL][Config][SessionOperationalRuntime] RuntimeConfigRegistry snapshot obrigatorio ausente para loading defaults migrados.");
        }

        public static OperationalRouteAsset ResolveStartupRouteOrFail(RuntimeModeConfig runtimeModeConfig)
        {
            if (runtimeModeConfig == null)
            {
                throw new InvalidOperationException("[FATAL][Config][SessionOperationalRuntime] RuntimeModeConfig obrigatorio ausente para resolver StartupRouteDefinition.");
            }

            if (RuntimeConfigRegistry.TryGetSnapshot(out IRuntimeConfigSnapshotReadOnly snapshot) && snapshot != null)
            {
                ISessionOperationalRuntimeConfigGroupReadOnly group = snapshot.SessionOperationalRuntime;
                if (group == null)
                {
                    throw new InvalidOperationException("[FATAL][Config][SessionOperationalRuntime] RuntimeConfigRegistry invariant breach: snapshot.SessionOperationalRuntime obrigatorio ausente.");
                }

                OperationalRouteAsset startupRoute = group.StartupRouteDefinition;
                if (startupRoute == null || !startupRoute.IsValid)
                {
                    throw new InvalidOperationException("[FATAL][Config][SessionOperationalRuntime] RuntimeConfigRegistry invariant breach: StartupRouteDefinition ausente/invalida no snapshot.");
                }

                LogStartupRouteSourceOnce(startupRoute);
                return startupRoute;
            }

            throw new InvalidOperationException("[FATAL][Config][SessionOperationalRuntime] RuntimeConfigRegistry snapshot obrigatorio ausente para startupRouteDefinition migrada.");
        }

        private static bool TryValidate(
            SessionOperationalRouteLoadingMode mode,
            RuntimeLoadingProfileAsset profile,
            out string errorMessage)
        {
            if (mode == SessionOperationalRouteLoadingMode.RuntimeDefault)
            {
                errorMessage = "defaultLoadingMode cannot be RuntimeDefault.";
                return false;
            }

            if (mode == SessionOperationalRouteLoadingMode.Profile)
            {
                if (profile == null)
                {
                    errorMessage = "defaultLoadingProfile is required when DefaultLoadingMode=Profile.";
                    return false;
                }

                if (!profile.TryValidate(out string profileValidationError))
                {
                    errorMessage = $"defaultLoadingProfile is invalid. detail='{profileValidationError}'.";
                    return false;
                }
            }

            errorMessage = string.Empty;
            return true;
        }

        private static void LogLoadingSourceOnce(
            SessionOperationalRouteLoadingMode mode,
            RuntimeLoadingProfileAsset profile)
        {
            if (_loadingConfigSourceLogged)
            {
                return;
            }

            _loadingConfigSourceLogged = true;
            string profileName = profile != null ? profile.name : "<none>";

            DebugUtility.Log(typeof(SessionOperationalRuntimeConfigResolver),
                $"[OBS][SessionOperationalRuntime][Config] loading defaults resolved via RuntimeConfigRegistry. defaultLoadingMode='{mode}' defaultLoadingProfile='{profileName}'.",
                DebugUtility.Colors.Info);
        }

        private static void LogStartupRouteSourceOnce(OperationalRouteAsset startupRoute)
        {
            if (_startupRouteSourceLogged)
            {
                return;
            }

            _startupRouteSourceLogged = true;
            string routeIdentity = startupRoute != null ? startupRoute.RouteIdentity : "<none>";

            DebugUtility.Log(typeof(SessionOperationalRuntimeConfigResolver),
                $"[OBS][SessionOperationalRuntime][Config] startupRouteDefinition resolved via RuntimeConfigRegistry. routeIdentity='{routeIdentity}'.",
                DebugUtility.Colors.Info);
        }
    }

    public readonly struct SessionOperationalRuntimeLoadingDefaults
    {
        public SessionOperationalRuntimeLoadingDefaults(
            SessionOperationalRouteLoadingMode mode,
            RuntimeLoadingProfileAsset profile)
        {
            Mode = mode;
            Profile = profile;
        }

        public SessionOperationalRouteLoadingMode Mode { get; }
        public RuntimeLoadingProfileAsset Profile { get; }
    }
}
