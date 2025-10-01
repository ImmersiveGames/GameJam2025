using System;
using System.Collections.Generic;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityUtils;
namespace _ImmersiveGames.Scripts.Utils.DependencySystems
{
    /// <summary>
    /// Gerencia serviços e injeções de dependência no escopo global, de cena e de objeto.
    /// </summary>
    [DisallowMultipleComponent]
    [DebugLevel(DebugLevel.Warning)]
    public class DependencyManager : RegulatorSingleton<DependencyManager>
    {
        [SerializeField] private int maxSceneServices = 2;
        [SerializeField] private bool useDontDestroyOnLoad = true;

        private DependencyInjector _injector;
        private ObjectServiceRegistry _objectRegistry;
        private SceneServiceRegistry _sceneRegistry;
        private GlobalServiceRegistry _globalRegistry;

        /// <summary>
        /// Define se o DependencyManager está em modo de teste (usado para testes unitários).
        /// </summary>
        public bool IsInTestMode { get; set; }

        /// <summary>
        /// Quantidade máxima de serviços registrados por cena.
        /// </summary>
        public int MaxSceneServices => maxSceneServices;

        /// <summary>
        /// Inicializa os registries.
        /// </summary>
        protected override void InitializeSingleton()
        {
            base.InitializeSingleton();
            
            // Só inicializa se esta for a instância ativa
            if (instance != this) return;

            _objectRegistry = new();
            _sceneRegistry = new(maxSceneServices);
            _globalRegistry = new();
            _injector = new(_objectRegistry, _sceneRegistry, _globalRegistry);

            DebugUtility.LogVerbose(typeof(DependencyManager), $"DependencyManager inicializado ({gameObject.scene.name}).", "yellow");
        }

        /// <summary> Registra um serviço no escopo global. </summary>
        public void RegisterGlobal<T>(T service) where T : class => _globalRegistry.Register(null, service);

        /// <summary> Tenta recuperar um serviço global. </summary>
        public bool TryGetGlobal<T>(out T service) where T : class => _globalRegistry.TryGet(null, out service);

        /// <summary>
        /// Registra um serviço no escopo de um objeto específico.
        /// </summary>
        /// <param name="objectId">ID do objeto.</param>
        /// <param name="service">Objeto do serviço para registrar</param>
        public void RegisterForObject<T>(string objectId, T service) where T : class
        {
            if (string.IsNullOrEmpty(objectId))
                throw new ArgumentNullException(nameof(objectId), "objectId é nulo ou vazio.");

            _objectRegistry.Register(objectId, service);
        }

        /// <summary> Tenta recuperar um serviço associado a um objeto. </summary>
        public bool TryGetForObject<T>(string objectId, out T service) where T : class => _objectRegistry.TryGet(objectId, out service);

        /// <summary>
        /// Registra um serviço associado a uma cena.
        /// </summary>
        public void RegisterForScene<T>(string sceneName, T service, bool allowOverride = false) where T : class =>
            _sceneRegistry.Register(sceneName, service, allowOverride);

        /// <summary> Tenta recuperar um serviço associado a uma cena. </summary>
        public bool TryGetForScene<T>(string sceneName, out T service) where T : class => _sceneRegistry.TryGet(sceneName, out service);

        /// <summary>
        /// Tenta recuperar um serviço verificando nos escopos: objeto, cena e global.
        /// </summary>
        public bool TryGet<T>(out T service, string objectId = null) where T : class
        {
            service = null;
            if (objectId != null && _objectRegistry.TryGet(objectId, out service) ||
                _sceneRegistry.TryGet(SceneManager.GetActiveScene().name, out service) ||
                (objectId == null && _globalRegistry.TryGet(null, out service)))
            {
                DebugUtility.LogVerbose(typeof(DependencyManager), $"Serviço {typeof(T).Name} encontrado.", "cyan");
                return true;
            }
            return false;
        }

        /// <summary>
        /// Injeta dependências em um objeto alvo.
        /// </summary>
        public void InjectDependencies(object target, string objectId = null)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            _injector.InjectDependencies(target, objectId);
        }

        /// <summary> Limpa todos os serviços de uma cena. </summary>
        public void ClearSceneServices(string sceneName) => _sceneRegistry.Clear(sceneName);

        /// <summary> Limpa todos os serviços de todas as cenas. </summary>
        public void ClearAllSceneServices() => _sceneRegistry.ClearAll();

        /// <summary> Limpa todos os serviços de um objeto específico. </summary>
        public void ClearObjectServices(string objectId) => _objectRegistry.Clear(objectId);

        /// <summary> Limpa todos os serviços de todos os objetos. </summary>
        public void ClearAllObjectServices() => _objectRegistry.ClearAll();

        /// <summary> Limpa todos os serviços globais. </summary>
        public void ClearGlobalServices() => _globalRegistry.Clear(null);

        /// <summary> Lista os serviços registrados para um objeto. </summary>
        public List<Type> ListServicesForObject(string objectId) => _objectRegistry.ListServices(objectId);

        /// <summary> Lista os serviços registrados para uma cena. </summary>
        public List<Type> ListServicesForScene(string sceneName) => _sceneRegistry.ListServices(sceneName);

        /// <summary> Lista os serviços registrados no escopo global. </summary>
        public List<Type> ListGlobalServices() => _globalRegistry.ListServices(null);
        
        
        /// <summary>
        /// Limpa todos os serviços quando o objeto é destruído.
        /// </summary>
        protected void OnDestroy()
        {
            // Só limpa se esta for a instância ativa
            if (instance == this)
            {
                ClearAllObjectServices();
                ClearAllSceneServices();
                ClearGlobalServices();
                StopAllCoroutines();
                
                DebugUtility.LogVerbose(typeof(DependencyManager), "DependencyManager destruído e serviços limpos.", "yellow");
            }
        }
        /// <summary>
        /// Chamado quando a aplicação está fechando.
        /// </summary>
        private void OnApplicationQuit()
        {
            // Limpeza adicional quando o jogo está fechando
            if (instance == this)
            {
                ClearAllObjectServices();
                ClearAllSceneServices();
                ClearGlobalServices();
                
                DebugUtility.LogVerbose(typeof(DependencyManager), "Serviços limpos no fechamento do jogo.", "yellow");
            }
        }
    }
}
