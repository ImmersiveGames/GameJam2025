#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace _ImmersiveGames.Scripts.Utils.Diagnostics.Editor
{
    /// <summary>
    /// Passo 0 (Diagnóstico): Gera um relatório Markdown com:
    /// - usos de Time.timeScale
    /// - usos de Find/Singleton/Instance
    /// - usos de EventBus/FilteredEventBus/EventBinding
    /// - registros em DependencyManager (Global/Scene/Object)
    /// - referências a Pooling (PoolManager/ObjectPool/PooledObject)
    ///
    /// Objetivo: facilitar inventário de dependências e pontos de risco
    /// sem alterar comportamento de runtime.
    /// </summary>
    public static class ProjectArchitectureAudit
    {
        // Ajuste aqui se quiser filtrar por pastas específicas
        private static readonly string[] DefaultSearchRoots =
        {
            "Assets"
        };

        private const string OutputRelativePath = "Assets/_ImmersiveGames/Docs/Generated/ArchitectureAudit.md";

        [MenuItem("ImmersiveGames/Diagnostics/Generate Architecture Audit (Step 0)")]
        public static void Generate()
        {
            try
            {
                var csFiles = CollectCsFiles(DefaultSearchRoots);
                if (csFiles.Count == 0)
                {
                    Debug.LogWarning("[ArchitectureAudit] Nenhum .cs encontrado em Assets.");
                    return;
                }

                var results = Analyze(csFiles);

                EnsureDirectoryFor(OutputRelativePath);

                File.WriteAllText(OutputRelativePath, BuildMarkdown(results), Encoding.UTF8);
                AssetDatabase.Refresh();

                Debug.Log($"[ArchitectureAudit] Relatório gerado em: {OutputRelativePath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ArchitectureAudit] Falha ao gerar relatório: {ex}");
            }
        }

        // ---------------------------
        // Coleta
        // ---------------------------

        private static List<string> CollectCsFiles(IEnumerable<string> roots)
        {
            var list = new List<string>(2048);

            foreach (var root in roots)
            {
                if (!Directory.Exists(root))
                    continue;

                var files = Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories);
                foreach (var f in files)
                {
                    // Ignora scripts gerados por packages, se por algum motivo estiverem em Assets
                    if (f.Replace("\\", "/").Contains("/Library/"))
                        continue;

                    list.Add(f.Replace("\\", "/"));
                }
            }

            return list;
        }

        // ---------------------------
        // Análise
        // ---------------------------

        private sealed class AuditResults
        {
            public int TotalFiles;

            public readonly Dictionary<string, FileFindings> FindingsByFile = new();

            public readonly List<string> TimeScaleFiles = new();
            public readonly List<string> FindUsageFiles = new();
            public readonly List<string> SingletonUsageFiles = new();
            public readonly List<string> EventBusUsageFiles = new();
            public readonly List<string> FilteredEventBusUsageFiles = new();
            public readonly List<string> DependencyRegistrationFiles = new();
            public readonly List<string> PoolingUsageFiles = new();

            public readonly Dictionary<string, int> PatternHitCounts = new();
        }

        private sealed class FileFindings
        {
            public string FilePath;
            public string Namespace;
            public readonly List<string> ClassNames = new();

            public readonly Dictionary<string, List<int>> PatternLines = new();
        }

        private static AuditResults Analyze(List<string> csFiles)
        {
            var results = new AuditResults { TotalFiles = csFiles.Count };

            // Padrões “arquiteturalmente relevantes”
            // Observação: padrões são intencionalmente simples/robustos (texto), sem parsing de AST.
            var patterns = new Dictionary<string, Regex>
            {
                // Tempo
                { "Time.timeScale", new Regex(@"\bTime\s*\.\s*timeScale\b", RegexOptions.Compiled) },

                // Find / reflection-ish Unity
                { "FindObjectOfType", new Regex(@"\bFindObjectOfType\s*<|\bFindObjectOfType\s*\(", RegexOptions.Compiled) },
                { "FindFirstObjectByType", new Regex(@"\bFindFirstObjectByType\s*<|\bFindFirstObjectByType\s*\(", RegexOptions.Compiled) },
                { "GameObject.Find", new Regex(@"\bGameObject\s*\.\s*Find\s*\(", RegexOptions.Compiled) },

                // Singleton/Instance
                { ".Instance", new Regex(@"\.\s*Instance\b", RegexOptions.Compiled) },
                { "SingletonBase", new Regex(@"\bSingleton\b|\bRegulatorSingleton\b", RegexOptions.Compiled) },

                // Event Bus
                { "EventBus.Register", new Regex(@"\bEventBus\s*<[^>]+>\s*\.\s*Register\s*\(", RegexOptions.Compiled) },
                { "EventBus.Unregister", new Regex(@"\bEventBus\s*<[^>]+>\s*\.\s*Unregister\s*\(", RegexOptions.Compiled) },
                { "EventBinding", new Regex(@"\bEventBinding\s*<", RegexOptions.Compiled) },

                // Filtered Bus (ActorId scoped)
                { "FilteredEventBus", new Regex(@"\bFilteredEventBus\b", RegexOptions.Compiled) },

                // DI registrations
                { "RegisterForGlobal", new Regex(@"\bRegisterForGlobal\s*<", RegexOptions.Compiled) },
                { "RegisterForScene", new Regex(@"\bRegisterForScene\s*<", RegexOptions.Compiled) },
                { "RegisterForObject", new Regex(@"\bRegisterForObject\s*<", RegexOptions.Compiled) },

                // Pooling
                { "PoolManager", new Regex(@"\bPoolManager\b", RegexOptions.Compiled) },
                { "ObjectPool", new Regex(@"\bObjectPool\b", RegexOptions.Compiled) },
                { "PooledObject", new Regex(@"\bPooledObject\b", RegexOptions.Compiled) },
            };

            foreach (var path in csFiles)
            {
                string text;
                try
                {
                    text = File.ReadAllText(path, Encoding.UTF8);
                }
                catch
                {
                    // fallback
                    text = File.ReadAllText(path);
                }

                var finding = new FileFindings
                {
                    FilePath = path,
                    Namespace = ExtractNamespace(text)
                };
                finding.ClassNames.AddRange(ExtractClassNames(text));

                // para reportar linhas, analisamos por linha
                var lines = SplitLines(text);

                foreach (var kv in patterns)
                {
                    var patternName = kv.Key;
                    var regex = kv.Value;

                    List<int> hitLines = null;

                    for (int i = 0; i < lines.Count; i++)
                    {
                        if (!regex.IsMatch(lines[i]))
                            continue;

                        hitLines ??= new List<int>(4);
                        hitLines.Add(i + 1);
                    }

                    if (hitLines != null)
                    {
                        finding.PatternLines[patternName] = hitLines;

                        if (!results.PatternHitCounts.ContainsKey(patternName))
                            results.PatternHitCounts[patternName] = 0;

                        results.PatternHitCounts[patternName] += hitLines.Count;
                    }
                }

                if (finding.PatternLines.Count > 0)
                    results.FindingsByFile[path] = finding;

                // Classificação por categoria (para listas rápidas)
                if (finding.PatternLines.ContainsKey("Time.timeScale"))
                    results.TimeScaleFiles.Add(path);

                if (finding.PatternLines.ContainsKey("FindObjectOfType") ||
                    finding.PatternLines.ContainsKey("FindFirstObjectByType") ||
                    finding.PatternLines.ContainsKey("GameObject.Find"))
                    results.FindUsageFiles.Add(path);

                if (finding.PatternLines.ContainsKey(".Instance") ||
                    finding.PatternLines.ContainsKey("SingletonBase"))
                    results.SingletonUsageFiles.Add(path);

                if (finding.PatternLines.ContainsKey("EventBus.Register") ||
                    finding.PatternLines.ContainsKey("EventBus.Unregister") ||
                    finding.PatternLines.ContainsKey("EventBinding"))
                    results.EventBusUsageFiles.Add(path);

                if (finding.PatternLines.ContainsKey("FilteredEventBus"))
                    results.FilteredEventBusUsageFiles.Add(path);

                if (finding.PatternLines.ContainsKey("RegisterForGlobal") ||
                    finding.PatternLines.ContainsKey("RegisterForScene") ||
                    finding.PatternLines.ContainsKey("RegisterForObject"))
                    results.DependencyRegistrationFiles.Add(path);

                if (finding.PatternLines.ContainsKey("PoolManager") ||
                    finding.PatternLines.ContainsKey("ObjectPool") ||
                    finding.PatternLines.ContainsKey("PooledObject"))
                    results.PoolingUsageFiles.Add(path);
            }

            // ordenação para estabilidade
            results.TimeScaleFiles.Sort(StringComparer.Ordinal);
            results.FindUsageFiles.Sort(StringComparer.Ordinal);
            results.SingletonUsageFiles.Sort(StringComparer.Ordinal);
            results.EventBusUsageFiles.Sort(StringComparer.Ordinal);
            results.FilteredEventBusUsageFiles.Sort(StringComparer.Ordinal);
            results.DependencyRegistrationFiles.Sort(StringComparer.Ordinal);
            results.PoolingUsageFiles.Sort(StringComparer.Ordinal);

            return results;
        }

        // ---------------------------
        // Markdown
        // ---------------------------

        private static string BuildMarkdown(AuditResults r)
        {
            var sb = new StringBuilder(32 * 1024);

            sb.AppendLine("# Architecture Audit (Step 0)");
            sb.AppendLine();
            sb.AppendLine($"Gerado em: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine();
            sb.AppendLine($"Total de arquivos analisados: **{r.TotalFiles}**");
            sb.AppendLine($"Arquivos com achados: **{r.FindingsByFile.Count}**");
            sb.AppendLine();

            sb.AppendLine("## Sumário de ocorrências (por padrão)");
            sb.AppendLine();
            foreach (var kv in r.PatternHitCounts.OrderByDescending(x => x.Value).ThenBy(x => x.Key, StringComparer.Ordinal))
            {
                sb.AppendLine($"- **{kv.Key}**: {kv.Value}");
            }

            sb.AppendLine();
            sb.AppendLine("## Listas rápidas (arquivos)");
            sb.AppendLine();

            AppendFileList(sb, "Time.timeScale", r.TimeScaleFiles);
            AppendFileList(sb, "Find usage (FindObjectOfType/FindFirstObjectByType/GameObject.Find)", r.FindUsageFiles);
            AppendFileList(sb, "Singleton/Instance usage", r.SingletonUsageFiles);
            AppendFileList(sb, "EventBus/EventBinding usage", r.EventBusUsageFiles);
            AppendFileList(sb, "FilteredEventBus usage", r.FilteredEventBusUsageFiles);
            AppendFileList(sb, "Dependency registrations (RegisterForGlobal/Scene/Object)", r.DependencyRegistrationFiles);
            AppendFileList(sb, "Pooling usage (PoolManager/ObjectPool/PooledObject)", r.PoolingUsageFiles);

            sb.AppendLine();
            sb.AppendLine("## Detalhe por arquivo (linhas)");
            sb.AppendLine();
            sb.AppendLine("> Observação: a numeração de linhas é aproximada ao arquivo atual; mudanças no arquivo mudam as linhas.");
            sb.AppendLine();

            foreach (var f in r.FindingsByFile.Values.OrderBy(x => x.FilePath, StringComparer.Ordinal))
            {
                sb.AppendLine($"### {ToProjectRelativePath(f.FilePath)}");
                if (!string.IsNullOrWhiteSpace(f.Namespace))
                    sb.AppendLine($"- Namespace: `{f.Namespace}`");
                if (f.ClassNames.Count > 0)
                    sb.AppendLine($"- Classes: {string.Join(", ", f.ClassNames.Select(c => $"`{c}`"))}");
                sb.AppendLine();

                foreach (var p in f.PatternLines.OrderBy(x => x.Key, StringComparer.Ordinal))
                {
                    sb.AppendLine($"- **{p.Key}**: linhas {string.Join(", ", p.Value)}");
                }

                sb.AppendLine();
            }

            sb.AppendLine("## Próximos passos sugeridos (para a Etapa 1+)");
            sb.AppendLine();
            sb.AppendLine("- Consolidar referências por domínio (OldActorRegistry / PlayerDomain / EaterDomain).");
            sb.AppendLine("- Reduzir dependências em `.Instance` e `Find*` nos consumidores de gameplay.");
            sb.AppendLine("- Separar FlowState / Gate (token-based) / TimePolicy antes de implementar reset in-place.");

            return sb.ToString();
        }

        private static void AppendFileList(StringBuilder sb, string title, List<string> files)
        {
            sb.AppendLine($"### {title}");
            if (files.Count == 0)
            {
                sb.AppendLine("- (nenhum)");
                sb.AppendLine();
                return;
            }

            foreach (var f in files)
                sb.AppendLine($"- {ToProjectRelativePath(f)}");

            sb.AppendLine();
        }

        // ---------------------------
        // Helpers (parsing leve)
        // ---------------------------

        private static string ExtractNamespace(string text)
        {
            // pega o primeiro namespace declarado
            var m = Regex.Match(text, @"\bnamespace\s+([A-Za-z0-9_\.]+)\s*\{", RegexOptions.Multiline);
            return m.Success ? m.Groups[1].Value.Trim() : string.Empty;
        }

        private static List<string> ExtractClassNames(string text)
        {
            // não é um parser completo; apenas “melhor esforço”
            // captura: class X, sealed class X, partial class X, etc.
            var list = new List<string>(4);

            foreach (Match m in Regex.Matches(text, @"\bclass\s+([A-Za-z0-9_]+)\b", RegexOptions.Multiline))
            {
                var name = m.Groups[1].Value.Trim();
                if (!string.IsNullOrWhiteSpace(name) && !list.Contains(name))
                    list.Add(name);
            }

            return list;
        }

        private static List<string> SplitLines(string text)
        {
            // lida com \r\n / \n
            return text.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n').ToList();
        }

        private static void EnsureDirectoryFor(string assetPath)
        {
            var dir = Path.GetDirectoryName(assetPath);
            if (string.IsNullOrWhiteSpace(dir))
                return;

            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
        }

        private static string ToProjectRelativePath(string fullPathOrAssetsPath)
        {
            var p = fullPathOrAssetsPath.Replace("\\", "/");
            // se já estiver em Assets/..., retorna
            var idx = p.IndexOf("Assets/", StringComparison.Ordinal);
            if (idx >= 0)
                return p.Substring(idx);

            return p;
        }
    }
}
#endif
