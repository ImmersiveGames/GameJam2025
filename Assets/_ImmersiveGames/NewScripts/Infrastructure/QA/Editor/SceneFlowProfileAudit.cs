#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Infrastructure.QA.Editor
{
    /// <summary>
    /// Auditoria automática de uso de SceneFlow profiles (sem depender de verificação manual).
    ///
    /// O que audita:
    /// - Ocorrências de new SceneTransitionRequest(...)
    /// - Ocorrências de new SceneTransitionContext(...) (criação direta de context)
    /// - Uso de SceneFlowProfileNames.*
    /// - String literals candidatas a profile dentro desses blocos
    /// - Heurística de assets em Resources relacionados a "profile"
    ///
    /// Saída:
    /// - Report Markdown em Assets/_ImmersiveGames/NewScripts/Docs/Reports/SceneFlow-Profile-Audit.md
    ///
    /// Execução:
    /// - Menu: Tools -> NewScripts -> Audit -> SceneFlow Profiles
    /// </summary>
    public static class SceneFlowProfileAudit
    {
        // Escopo padrão: apenas NewScripts (evita poluir com legado).
        private const string DefaultScanRoot = "Assets/_ImmersiveGames/NewScripts";

        // Report padrão no Docs/Reports (conforme convenção do projeto).
        private const string DefaultReportPath = "Assets/_ImmersiveGames/NewScripts/Docs/Reports/SceneFlow-Profile-Audit.md";

        // Profiles canônicos de produção (mantidos aqui para validação objetiva do audit).
        // Se o projeto mudar esses nomes, ajuste esta lista (o report indica isso explicitamente).
        private static readonly HashSet<string> CanonicalProductionProfiles =
            new HashSet<string>(StringComparer.Ordinal)
            {
                "startup",
                "frontend",
                "gameplay"
            };

        // Prefixo aceito para QA profiles (não falha o audit; apenas classifica).
        private const string QaProfilePrefix = "qa.";

        // Mantém a heurística de Resources ligada por padrão; pode ser desativada se o projeto não usa assets de profile.
        private const bool EnableResourcesHeuristic = true;

        // Fallback defensivo: evita que a própria tool apareça no scan caso haja comportamento inesperado.
        private const string SelfFileName = "SceneFlowProfileAudit.cs";

        [MenuItem("Tools/NewScripts/Audit/SceneFlow Profiles")]
        public static void RunFromMenu()
        {
            RunAuditAndWriteReport(
                scanRoot: DefaultScanRoot,
                reportPath: DefaultReportPath,
                includeSubfolders: true);
        }

        /// <summary>
        /// Entry-point chamável por CI/batch via Editor (ex.: método estático).
        /// </summary>
        public static void RunAuditAndWriteReport(string scanRoot, string reportPath, bool includeSubfolders)
        {
            try
            {
                var result = RunAudit(scanRoot, includeSubfolders);
                WriteReport(reportPath, scanRoot, result);

                Debug.Log($"[SceneFlowProfileAudit] Report gerado: {reportPath}");
                AssetDatabase.Refresh();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SceneFlowProfileAudit] Falha ao executar auditoria: {ex.GetType().Name}: {ex.Message}\n{ex}");
            }
        }

        private static AuditResult RunAudit(string scanRoot, bool includeSubfolders)
        {
            var result = new AuditResult();

            // 1) Auditar assets em Resources (heurística por nome).
            result.ResourceProfileAssets = EnableResourcesHeuristic
                ? FindResourceProfileAssets(scanRoot)
                : new List<string>();

            // 2) Auditar código.
            var csFiles = CollectCsFiles(scanRoot, includeSubfolders);
            foreach (var file in csFiles)
            {
                ScanFile(file, result);
            }

            // 3) Consolidar e classificar profiles encontrados.
            result.UniqueProfileLiterals = result.ProfileLiteralOccurrences
                .Select(o => o.ProfileLiteral)
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(p => p, StringComparer.Ordinal)
                .ToList();

            result.CanonicalProfilesMissingInResources = EnableResourcesHeuristic
                ? CanonicalProductionProfiles
                    .Where(p => !ContainsProfileAssetHeuristic(result.ResourceProfileAssets, p))
                    .OrderBy(p => p, StringComparer.Ordinal)
                    .ToList()
                : new List<string>();

            result.UnexpectedProductionProfileLiterals = result.UniqueProfileLiterals
                .Where(p => !CanonicalProductionProfiles.Contains(p) && !p.StartsWith(QaProfilePrefix, StringComparison.Ordinal))
                .OrderBy(p => p, StringComparer.Ordinal)
                .ToList();

            return result;
        }

        private static List<string> CollectCsFiles(string scanRoot, bool includeSubfolders)
        {
            if (string.IsNullOrWhiteSpace(scanRoot) || !Directory.Exists(scanRoot))
            {
                return new List<string>();
            }

            var option = includeSubfolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            return Directory.GetFiles(scanRoot, "*.cs", option)
                .Where(p => !p.Contains("/EditorGenerated/", StringComparison.OrdinalIgnoreCase))
                .Where(p => !p.EndsWith("/" + SelfFileName, StringComparison.OrdinalIgnoreCase))
                .Where(p => !p.EndsWith("\\" + SelfFileName, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        private static void ScanFile(string path, AuditResult result)
        {
            string text;
            try
            {
                text = File.ReadAllText(path, Encoding.UTF8);
            }
            catch
            {
                return;
            }

            // Localização: calculamos line/col via índice => line mapping básico.
            var lineIndex = BuildLineIndex(text);

            // Criamos um "mask" do texto que remove comentários e literais de string/char.
            // Isso impede que tokens "new SceneTransitionContext(" em XML docs ou comments gerem falsos positivos.
            var masked = BuildMaskedText(text);

            // 1) Capturar blocos de new SceneTransitionRequest(...)
            foreach (var block in ExtractConstructorBlocks(text, masked, "SceneTransitionRequest"))
            {
                var occ = new ConstructorOccurrence
                {
                    FilePath = path,
                    StartIndex = block.StartIndex,
                    EndIndex = block.EndIndex,
                    Snippet = block.Snippet
                };

                FillLocation(occ, lineIndex);

                occ.ExtractedProfileLiterals = ExtractStringLiterals(block.Snippet);
                result.SceneTransitionRequestOccurrences.Add(occ);

                // Registramos todos os literais; a classificação final é por valor.
                if (occ.ExtractedProfileLiterals.Count > 0)
                {
                    foreach (var lit in occ.ExtractedProfileLiterals)
                    {
                        if (LooksLikeProfile(lit))
                        {
                            result.ProfileLiteralOccurrences.Add(new ProfileLiteralOccurrence
                            {
                                FilePath = occ.FilePath,
                                Line = occ.Line,
                                Column = occ.Column,
                                Container = "SceneTransitionRequest",
                                ProfileLiteral = lit
                            });
                        }
                    }
                }
            }

            // 2) Capturar blocos de new SceneTransitionContext(...)
            foreach (var block in ExtractConstructorBlocks(text, masked, "SceneTransitionContext"))
            {
                var occ = new ConstructorOccurrence
                {
                    FilePath = path,
                    StartIndex = block.StartIndex,
                    EndIndex = block.EndIndex,
                    Snippet = block.Snippet
                };

                FillLocation(occ, lineIndex);

                occ.ExtractedProfileLiterals = ExtractStringLiterals(block.Snippet);
                result.SceneTransitionContextOccurrences.Add(occ);

                foreach (var lit in occ.ExtractedProfileLiterals)
                {
                    if (LooksLikeProfile(lit))
                    {
                        result.ProfileLiteralOccurrences.Add(new ProfileLiteralOccurrence
                        {
                            FilePath = occ.FilePath,
                            Line = occ.Line,
                            Column = occ.Column,
                            Container = "SceneTransitionContext",
                            ProfileLiteral = lit
                        });
                    }
                }
            }

            // 3) Detectar usos de SceneFlowProfileNames.* (pode aparecer em comments; aqui não é crítico, mas mascaramos também)
            foreach (Match m in Regex.Matches(masked, @"\bSceneFlowProfileNames\s*\.\s*([A-Za-z_][A-Za-z0-9_]*)", RegexOptions.Multiline))
            {
                var idx = m.Index;
                var (line, col) = GetLineCol(lineIndex, idx);
                result.SceneFlowProfileNamesUsages.Add(new SymbolUsage
                {
                    FilePath = path,
                    Line = line,
                    Column = col,
                    Symbol = "SceneFlowProfileNames." + m.Groups[1].Value
                });
            }
        }

        private static bool LooksLikeProfile(string literal)
        {
            if (string.IsNullOrWhiteSpace(literal))
            {
                return false;
            }

            // Profiles conhecidos (prod)
            if (CanonicalProductionProfiles.Contains(literal))
            {
                return true;
            }

            // QA prefix
            if (literal.StartsWith(QaProfilePrefix, StringComparison.Ordinal))
            {
                return true;
            }

            // Heurística: muitos profiles são tokens "lowercase + underscore/dot/hyphen"
            for (int i = 0; i < literal.Length; i++)
            {
                char c = literal[i];
                bool ok = (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '_' || c == '.' || c == '-';
                if (!ok)
                {
                    return false;
                }
            }

            // Tamanho mínimo para reduzir falsos positivos
            return literal.Length >= 3;
        }

        private static List<string> ExtractStringLiterals(string snippet)
        {
            // Extrai literais C# básicos: "..." e @"..." e $"..." (captura o conteúdo textual).
            // Isso é suficiente para capturar profiles canônicos e tokens qa.*.
            var list = new List<string>();
            if (string.IsNullOrEmpty(snippet))
            {
                return list;
            }

            int i = 0;
            while (i < snippet.Length)
            {
                char c = snippet[i];

                // Ignora comentários dentro do snippet (edge cases com inline comments).
                if (c == '/' && i + 1 < snippet.Length)
                {
                    if (snippet[i + 1] == '/')
                    {
                        i += 2;
                        while (i < snippet.Length && snippet[i] != '\n') i++;
                        continue;
                    }

                    if (snippet[i + 1] == '*')
                    {
                        i += 2;
                        while (i + 1 < snippet.Length && !(snippet[i] == '*' && snippet[i + 1] == '/')) i++;
                        i = Math.Min(i + 2, snippet.Length);
                        continue;
                    }
                }

                // Detecta início de string:
                // - "..."
                // - @"..."
                // - $"..."
                // - $@"..."
                bool isVerbatim = false;

                if (c == '@' && i + 1 < snippet.Length && snippet[i + 1] == '"')
                {
                    isVerbatim = true;
                    i++; // avança para a aspas
                    c = snippet[i];
                }
                else if (c == '$' && i + 1 < snippet.Length)
                {
                    // $"..."
                    if (snippet[i + 1] == '"')
                    {
                        i++; // avança para a aspas
                        c = snippet[i];
                    }
                    // $@"..."
                    else if (i + 2 < snippet.Length && snippet[i + 1] == '@' && snippet[i + 2] == '"')
                    {
                        isVerbatim = true;
                        i += 2; // avança para a aspas
                        c = snippet[i];
                    }
                }

                if (c == '"')
                {
                    i++; // depois da abertura
                    var sb = new StringBuilder();

                    if (isVerbatim)
                    {
                        while (i < snippet.Length)
                        {
                            if (snippet[i] == '"')
                            {
                                // "" => escape em verbatim
                                if (i + 1 < snippet.Length && snippet[i + 1] == '"')
                                {
                                    sb.Append('"');
                                    i += 2;
                                    continue;
                                }

                                i++; // fecha
                                break;
                            }

                            sb.Append(snippet[i]);
                            i++;
                        }
                    }
                    else
                    {
                        while (i < snippet.Length)
                        {
                            if (snippet[i] == '\\' && i + 1 < snippet.Length)
                            {
                                // Mantém conteúdo aproximado (não precisa interpretar escapes para profiles)
                                sb.Append(snippet[i + 1]);
                                i += 2;
                                continue;
                            }

                            if (snippet[i] == '"')
                            {
                                i++; // fecha
                                break;
                            }

                            sb.Append(snippet[i]);
                            i++;
                        }
                    }

                    list.Add(sb.ToString());
                    continue;
                }

                i++;
            }

            return list;
        }

        private static List<string> FindResourceProfileAssets(string scanRoot)
        {
            // Heurística: procura por assets que contenham "profile" no nome, dentro do scanRoot.
            var results = new List<string>();

            try
            {
                var guids = AssetDatabase.FindAssets("profile", new[] { scanRoot });
                foreach (var g in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(g);
                    if (string.IsNullOrEmpty(path))
                    {
                        continue;
                    }

                    if (path.IndexOf("/Resources/", StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        continue;
                    }

                    results.Add(path);
                }
            }
            catch
            {
                // Ignora falhas de AssetDatabase em contextos específicos.
            }

            return results
                .Distinct(StringComparer.Ordinal)
                .OrderBy(p => p, StringComparer.Ordinal)
                .ToList();
        }

        private static bool ContainsProfileAssetHeuristic(List<string> resourceAssets, string profileName)
        {
            if (resourceAssets == null || resourceAssets.Count == 0)
            {
                return false;
            }

            foreach (var path in resourceAssets)
            {
                var file = Path.GetFileNameWithoutExtension(path);
                if (string.Equals(file, profileName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                if (!string.IsNullOrEmpty(file) &&
                    file.IndexOf(profileName, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static void WriteReport(string reportPath, string scanRoot, AuditResult result)
        {
            var dir = Path.GetDirectoryName(reportPath);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var sb = new StringBuilder();

            sb.AppendLine("# SceneFlow Profile Audit");
            sb.AppendLine($"- Timestamp (UTC): {DateTime.UtcNow:O}");
            sb.AppendLine($"- Scan root: `{scanRoot}`");
            sb.AppendLine($"- Canonical production profiles (tool config): `{string.Join(", ", CanonicalProductionProfiles.OrderBy(x => x))}`");
            sb.AppendLine($"- QA profile prefix (tool config): `{QaProfilePrefix}`");
            sb.AppendLine($"- Resources heuristic enabled: `{EnableResourcesHeuristic}`");
            sb.AppendLine();

            // Resources
            sb.AppendLine("## Resources profile assets (heurística)");
            if (!EnableResourcesHeuristic)
            {
                sb.AppendLine("- (heurística desativada na tool; EnableResourcesHeuristic=false)");
            }
            else if (result.ResourceProfileAssets.Count == 0)
            {
                sb.AppendLine("- (nenhum asset encontrado via `AssetDatabase.FindAssets(\"profile\")` dentro de Resources)");
            }
            else
            {
                foreach (var p in result.ResourceProfileAssets)
                {
                    sb.AppendLine($"- `{p}`");
                }
            }
            sb.AppendLine();

            sb.AppendLine("### Canonical profiles missing in Resources (heurística)");
            if (!EnableResourcesHeuristic)
            {
                sb.AppendLine("- (heurística desativada)");
            }
            else if (result.CanonicalProfilesMissingInResources.Count == 0)
            {
                sb.AppendLine("- (nenhum faltando detectado pela heurística)");
            }
            else
            {
                foreach (var p in result.CanonicalProfilesMissingInResources)
                {
                    sb.AppendLine($"- `{p}`");
                }

                sb.AppendLine();
                sb.AppendLine("> Observação: esta checagem é heurística por nome do asset. Se seus profiles não são assets em Resources, ignore este bloco (ou desligue a heurística).");
            }
            sb.AppendLine();

            // Code occurrences summary
            sb.AppendLine("## Code scan summary");
            sb.AppendLine($"- SceneTransitionRequest occurrences: `{result.SceneTransitionRequestOccurrences.Count}`");
            sb.AppendLine($"- SceneTransitionContext direct occurrences: `{result.SceneTransitionContextOccurrences.Count}`");
            sb.AppendLine($"- SceneFlowProfileNames usages: `{result.SceneFlowProfileNamesUsages.Count}`");
            sb.AppendLine($"- Profile-like string literals captured: `{result.ProfileLiteralOccurrences.Count}`");
            sb.AppendLine();

            // Profiles extracted
            sb.AppendLine("## Unique profile literals found");
            if (result.UniqueProfileLiterals.Count == 0)
            {
                sb.AppendLine("- (nenhum literal classificado como profile foi encontrado)");
            }
            else
            {
                foreach (var p in result.UniqueProfileLiterals)
                {
                    var cls = ClassifyProfile(p);
                    sb.AppendLine($"- `{p}` ({cls})");
                }
            }
            sb.AppendLine();

            sb.AppendLine("### Unexpected non-canonical profile literals (não QA)");
            if (result.UnexpectedProductionProfileLiterals.Count == 0)
            {
                sb.AppendLine("- (nenhum detectado)");
            }
            else
            {
                foreach (var p in result.UnexpectedProductionProfileLiterals)
                {
                    sb.AppendLine($"- `{p}`");
                }

                sb.AppendLine();
                sb.AppendLine("> Ação: se estes forem intencionais, adote prefixo `qa.` (se forem QA) ou adicione ao set canônico desta tool (se forem produção).");
            }
            sb.AppendLine();

            // Detailed occurrences
            sb.AppendLine("## Detail: Profile literal occurrences (por arquivo/linha)");
            if (result.ProfileLiteralOccurrences.Count == 0)
            {
                sb.AppendLine("- (nenhuma ocorrência)");
            }
            else
            {
                foreach (var occ in result.ProfileLiteralOccurrences
                             .OrderBy(o => o.FilePath, StringComparer.Ordinal)
                             .ThenBy(o => o.Line)
                             .ThenBy(o => o.Column))
                {
                    sb.AppendLine($"- `{occ.ProfileLiteral}` in **{occ.Container}** at `{occ.FilePath}:{occ.Line}:{occ.Column}`");
                }
            }
            sb.AppendLine();

            sb.AppendLine("## Detail: SceneTransitionRequest blocks");
            WriteConstructorOccurrences(sb, result.SceneTransitionRequestOccurrences);

            sb.AppendLine();
            sb.AppendLine("## Detail: SceneTransitionContext blocks (criação direta)");
            WriteConstructorOccurrences(sb, result.SceneTransitionContextOccurrences);

            sb.AppendLine();
            sb.AppendLine("## Detail: SceneFlowProfileNames.* usages");
            if (result.SceneFlowProfileNamesUsages.Count == 0)
            {
                sb.AppendLine("- (nenhum uso encontrado)");
            }
            else
            {
                foreach (var u in result.SceneFlowProfileNamesUsages
                             .OrderBy(x => x.FilePath, StringComparer.Ordinal)
                             .ThenBy(x => x.Line)
                             .ThenBy(x => x.Column))
                {
                    sb.AppendLine($"- `{u.Symbol}` at `{u.FilePath}:{u.Line}:{u.Column}`");
                }
            }

            File.WriteAllText(reportPath, sb.ToString(), Encoding.UTF8);
        }

        private static string ClassifyProfile(string p)
        {
            if (CanonicalProductionProfiles.Contains(p)) return "canonical-production";
            if (p.StartsWith(QaProfilePrefix, StringComparison.Ordinal)) return "qa";
            return "other";
        }

        private static void WriteConstructorOccurrences(StringBuilder sb, List<ConstructorOccurrence> occurrences)
        {
            if (occurrences.Count == 0)
            {
                sb.AppendLine("- (nenhuma ocorrência)");
                return;
            }

            foreach (var occ in occurrences
                     .OrderBy(o => o.FilePath, StringComparer.Ordinal)
                     .ThenBy(o => o.Line)
                     .ThenBy(o => o.Column))
            {
                sb.AppendLine($"- `{occ.FilePath}:{occ.Line}:{occ.Column}`");
                if (occ.ExtractedProfileLiterals.Count > 0)
                {
                    sb.AppendLine($"  - string literals: `{string.Join(", ", occ.ExtractedProfileLiterals.Select(x => $"\"{x}\""))}`");
                }

                // Snippet curto (evita arquivo gigantesco).
                var snippet = CompactSnippet(occ.Snippet, maxChars: 240);
                sb.AppendLine($"  - snippet: `{snippet}`");
            }
        }

        private static string CompactSnippet(string snippet, int maxChars)
        {
            if (string.IsNullOrEmpty(snippet))
            {
                return string.Empty;
            }

            var compact = Regex.Replace(snippet, @"\s+", " ").Trim();
            if (compact.Length <= maxChars)
            {
                return compact;
            }

            return compact.Substring(0, maxChars) + "...";
        }

        private static void FillLocation(ConstructorOccurrence occ, List<int> lineIndex)
        {
            var (line, col) = GetLineCol(lineIndex, occ.StartIndex);
            occ.Line = line;
            occ.Column = col;
        }

        private static List<int> BuildLineIndex(string text)
        {
            // lineIndex[i] = start char index of line i (0-based lines)
            var list = new List<int>(capacity: 4096) { 0 };

            if (string.IsNullOrEmpty(text))
            {
                return list;
            }

            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '\n')
                {
                    list.Add(i + 1);
                }
            }

            return list;
        }

        private static (int line, int col) GetLineCol(List<int> lineIndex, int charIndex)
        {
            if (lineIndex == null || lineIndex.Count == 0)
            {
                return (1, 1);
            }

            // Binary search: last line start <= charIndex
            int lo = 0, hi = lineIndex.Count - 1, best = 0;
            while (lo <= hi)
            {
                int mid = (lo + hi) / 2;
                int start = lineIndex[mid];
                if (start <= charIndex)
                {
                    best = mid;
                    lo = mid + 1;
                }
                else
                {
                    hi = mid - 1;
                }
            }

            int lineStart = lineIndex[best];
            int line = best + 1;                    // 1-based
            int col = (charIndex - lineStart) + 1;  // 1-based
            return (line, col);
        }

        private static IEnumerable<ConstructorBlock> ExtractConstructorBlocks(string originalText, string maskedText, string typeName)
        {
            // Procura "new <typeName>(" e captura até o fechamento correspondente de parênteses + ";"
            // IMPORTANT: usa maskedText (sem comentários/strings) para evitar falsos positivos em docs/comments.
            if (string.IsNullOrEmpty(maskedText))
            {
                yield break;
            }

            string token = "new " + typeName + "(";
            int idx = 0;

            while (idx < maskedText.Length)
            {
                int start = maskedText.IndexOf(token, idx, StringComparison.Ordinal);
                if (start < 0)
                {
                    yield break;
                }

                int parenStart = start + token.Length - 1; // posição do '('
                int end = FindMatchingParenEnd(maskedText, parenStart);
                if (end < 0)
                {
                    idx = start + token.Length;
                    continue;
                }

                // Expandir até ';' se existir próximo.
                int semi = maskedText.IndexOf(';', end);
                int blockEnd = semi > 0 ? semi : end;

                int length = Math.Min(blockEnd - start + 1, originalText.Length - start);
                var snippet = originalText.Substring(start, length);

                yield return new ConstructorBlock
                {
                    StartIndex = start,
                    EndIndex = start + length - 1,
                    Snippet = snippet
                };

                idx = start + length;
            }
        }

        private static int FindMatchingParenEnd(string maskedText, int parenStartIndex)
        {
            if (parenStartIndex < 0 || parenStartIndex >= maskedText.Length || maskedText[parenStartIndex] != '(')
            {
                return -1;
            }

            int depth = 0;

            for (int i = parenStartIndex; i < maskedText.Length; i++)
            {
                char c = maskedText[i];
                if (c == '(') depth++;
                if (c == ')')
                {
                    depth--;
                    if (depth == 0)
                    {
                        return i;
                    }
                }
            }

            return -1;
        }

        private static string BuildMaskedText(string text)
        {
            // Retorna um texto com mesmo tamanho, onde:
            // - comentários (// e /* */) viram espaços
            // - literais de string/char viram espaços
            // Mantém '\n' para preservar a estrutura visual; índices permanecem iguais.
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            var chars = text.ToCharArray();

            bool inLineComment = false;
            bool inBlockComment = false;
            bool inString = false;
            bool inVerbatimString = false;
            bool inChar = false;

            for (int i = 0; i < chars.Length; i++)
            {
                char c = chars[i];
                char next = i + 1 < chars.Length ? chars[i + 1] : '\0';

                // Finaliza line comment
                if (inLineComment)
                {
                    if (c != '\n')
                    {
                        chars[i] = ' ';
                        continue;
                    }

                    inLineComment = false;
                    continue;
                }

                // Finaliza block comment
                if (inBlockComment)
                {
                    if (c == '*' && next == '/')
                    {
                        chars[i] = ' ';
                        chars[i + 1] = ' ';
                        i++;
                        inBlockComment = false;
                        continue;
                    }

                    if (c != '\n')
                    {
                        chars[i] = ' ';
                    }
                    continue;
                }

                // String literal
                if (inString)
                {
                    if (c == '\\' && !inVerbatimString)
                    {
                        // escape: mascara este e o próximo
                        if (c != '\n') chars[i] = ' ';
                        if (i + 1 < chars.Length && chars[i + 1] != '\n') chars[i + 1] = ' ';
                        i++;
                        continue;
                    }

                    if (c == '"' )
                    {
                        if (inVerbatimString && next == '"')
                        {
                            // "" em verbatim
                            if (c != '\n') chars[i] = ' ';
                            if (next != '\n') chars[i + 1] = ' ';
                            i++;
                            continue;
                        }

                        if (c != '\n') chars[i] = ' ';
                        inString = false;
                        inVerbatimString = false;
                        continue;
                    }

                    if (c != '\n') chars[i] = ' ';
                    continue;
                }

                // Char literal
                if (inChar)
                {
                    if (c == '\\')
                    {
                        if (c != '\n') chars[i] = ' ';
                        if (i + 1 < chars.Length && chars[i + 1] != '\n') chars[i + 1] = ' ';
                        i++;
                        continue;
                    }

                    if (c == '\'')
                    {
                        if (c != '\n') chars[i] = ' ';
                        inChar = false;
                        continue;
                    }

                    if (c != '\n') chars[i] = ' ';
                    continue;
                }

                // Entradas (apenas quando estamos em "normal")
                if (c == '/' && next == '/')
                {
                    chars[i] = ' ';
                    chars[i + 1] = ' ';
                    i++;
                    inLineComment = true;
                    continue;
                }

                if (c == '/' && next == '*')
                {
                    chars[i] = ' ';
                    chars[i + 1] = ' ';
                    i++;
                    inBlockComment = true;
                    continue;
                }

                // Início de char literal
                if (c == '\'')
                {
                    chars[i] = ' ';
                    inChar = true;
                    continue;
                }

                // Início de string:
                // - "..."
                // - @"..."
                // - $"..."
                // - $@"..."
                if (c == '"')
                {
                    chars[i] = ' ';
                    inString = true;
                    inVerbatimString = (i > 0 && chars[i - 1] == '@');
                    continue;
                }

                if (c == '@' && next == '"')
                {
                    chars[i] = ' ';
                    // a aspas será processada no próximo i e abrirá string; marcamos verbatim pelo '@' anterior.
                    continue;
                }

                if (c == '$' && (next == '"' || (next == '@' && i + 2 < chars.Length && chars[i + 2] == '"')))
                {
                    chars[i] = ' ';
                    continue;
                }
            }

            return new string(chars);
        }

        private sealed class AuditResult
        {
            public List<string> ResourceProfileAssets { get; set; } = new List<string>();

            public List<ConstructorOccurrence> SceneTransitionRequestOccurrences { get; } = new List<ConstructorOccurrence>();
            public List<ConstructorOccurrence> SceneTransitionContextOccurrences { get; } = new List<ConstructorOccurrence>();

            public List<SymbolUsage> SceneFlowProfileNamesUsages { get; } = new List<SymbolUsage>();

            public List<ProfileLiteralOccurrence> ProfileLiteralOccurrences { get; } = new List<ProfileLiteralOccurrence>();
            public List<string> UniqueProfileLiterals { get; set; } = new List<string>();

            public List<string> CanonicalProfilesMissingInResources { get; set; } = new List<string>();
            public List<string> UnexpectedProductionProfileLiterals { get; set; } = new List<string>();
        }

        private sealed class ConstructorOccurrence
        {
            public string FilePath;
            public int StartIndex;
            public int EndIndex;
            public int Line;
            public int Column;
            public string Snippet;

            public List<string> ExtractedProfileLiterals = new List<string>();
        }

        private sealed class ProfileLiteralOccurrence
        {
            public string FilePath;
            public int Line;
            public int Column;
            public string Container;
            public string ProfileLiteral;
        }

        private sealed class SymbolUsage
        {
            public string FilePath;
            public int Line;
            public int Column;
            public string Symbol;
        }

        private sealed class ConstructorBlock
        {
            public int StartIndex;
            public int EndIndex;
            public string Snippet;
        }
    }
}
#endif
