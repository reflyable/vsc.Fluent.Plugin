using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using Blast.API.Search;
using Blast.Core;
using Blast.Core.Interfaces;
using Blast.Core.Objects;
using Blast.Core.Results;
using Fastenshtein;
using vsc.Fluent.Plugin.Properties;
using vsc.Fluent.Plugin.RemoteMachinesHelper;
using vsc.Fluent.Plugin.VSCodeHelper;
using vsc.Fluent.Plugin.WorkspacesHelper;

namespace vsc.Fluent.Plugin
{
    public class VSCodeWorkspacesSearchApp : ISearchApplication
    {
        private const string SearchTag = "vsc";
        private const string SearchAppName = "VSCodeWorkspace";
        private readonly SearchApplicationInfo _applicationInfo;
        private readonly List<ISearchOperation> _supportedOperations;
        private readonly VSCodeWorkspacesApi _workspacesApi = new();

        private readonly VSCodeRemoteMachinesApi _machinesApi = new();
        public VSCodeWorkspacesSearchApp()
        {
            _supportedOperations = new List<ISearchOperation>
            {
                new VSCodeWorkspacesSearchOperation()
            };
            VSCodeInstances.LoadVSCodeInstances();
            _applicationInfo = new SearchApplicationInfo(SearchAppName,
                "This apps opens VSCode workspaces", _supportedOperations)
            {
                MinimumSearchLength = 1,
                IsProcessSearchEnabled = false,
                IsProcessSearchOffline = false,
                ApplicationIconGlyph = "\uE943",
                SearchAllTime = ApplicationSearchTime.Fast,
                DefaultSearchTags = new List<SearchTag>()
            };
        }
        public ValueTask LoadSearchApplicationAsync()
        {
            return ValueTask.CompletedTask;
            // This is used if you need to load anything asynchronously on Fluent Search startup
        }
        public SearchApplicationInfo GetApplicationInfo()
        {
            return _applicationInfo;
        }
        private bool IsStartOfToken(string data, int index)
        {
            if (index == 0 && data.Length != 1)
            {
                return true;
            }

            if (data.Length == 1)
            {
                return false;
            }

            char c = data[index - 1];
            if (c != ' ' && c != '.' && c != '\\')
            {
                if (char.IsUpper(data[index]))
                {
                    return !char.IsUpper(c);
                }

                return false;
            }

            return true;
        }
        private double getscore(string data,string search)
        {
            if (string.IsNullOrEmpty(data))
            {
                return 0.0;
            }

            bool flag = false;
          

            ReadOnlySpan<char> readOnlySpan = data.AsSpan();
            ReadOnlySpan<char> readOnlySpan2 = search.AsSpan();
            int num = readOnlySpan.IndexOf(search, StringComparison.OrdinalIgnoreCase);
            switch (num)
            {
                case 0:
                    if (readOnlySpan.Length == readOnlySpan2.Length)
                    {
                        return 16.0;
                    }

                    return 8.0 + 8.0 * (double)readOnlySpan2.Length / (double)readOnlySpan.Length;
                default:
                    if (readOnlySpan.IsStartOfToken(num))
                    {
                        return 6.5 + 6.5 * (double)readOnlySpan2.Length / (double)readOnlySpan.Length;
                    }

                    return 5.5 + 5.5 * (double)readOnlySpan2.Length / (double)readOnlySpan.Length;
                case -1:
                    {
                        double num2 = 0.0;
                        int num3 = 0;
                        int num4 = -1;
                        int num5 = 0;
                        bool flag2 =  !search.Contains(' ');
                        int num6 = (flag2 ? 4 : 3);
                        int num7 = (flag2 ? 3 : 2);
                        int num8 = 0;
                        if (flag2)
                        {
                            for (int i = 0; i < data.Length; i++)
                            {
                                if (IsStartOfToken(data, i))
                                {
                                    num8++;
                                }
                            }
                        }

                        bool flag3 = true;
                        bool flag4 = true;
                        int num9 = 0;
                        bool flag5 = false;
                        SpanSplitEnumerator<char> enumerator = readOnlySpan2.Split(flag2).GetEnumerator();
                        while (enumerator.MoveNext())
                        {
                            Range current = enumerator.Current;
                            ReadOnlySpan<char> readOnlySpan3 = readOnlySpan2;
                            ReadOnlySpan<char> value = readOnlySpan3[current.Start..current.End];
                            int num10;
                            if (!flag2 || num4 == -1)
                            {
                                num10 = readOnlySpan.IndexOf(value, StringComparison.OrdinalIgnoreCase);
                            }
                            else
                            {
                                readOnlySpan3 = readOnlySpan;
                                num10 = readOnlySpan3[(num4 + 1)..].IndexOf(value, StringComparison.OrdinalIgnoreCase);
                            }

                            int num11 = num10;
                            bool flag6 = num11 != -1;
                            if (flag6 && flag2 && num4 != -1)
                            {
                                num11 += num4 + 1;
                            }

                            if (!flag2 && value.Length <= 2 && (num11 <= num4 || num4 + num5 == num11))
                            {
                                return 0.0;
                            }

                            if (num11 != -1)
                            {
                                num5 += value.Length;
                            }

                            if (num11 <= num4)
                            {
                                if (flag2)
                                {
                                    if (!flag3)
                                    {
                                        return 0.0;
                                    }

                                    flag3 = false;
                                }

                                num2 -= 1.0;
                            }

                            if (flag6)
                            {
                                num2 += 2.5;
                                flag4 = true;
                                if (num11 == 0)
                                {
                                    num2 += (double)num6;
                                }
                                else if (readOnlySpan.IsStartOfToken(num11))
                                {
                                    num2 += (double)num7;
                                }
                                else if (flag2 && num11 - num4 > 1)
                                {
                                    readOnlySpan3 = readOnlySpan;
                                    int num12 = readOnlySpan3[new Range(end: readOnlySpan3.Length, start: num11 + 1)].IndexOf(value, StringComparison.OrdinalIgnoreCase);
                                    int num13 = num12 + num11 + 1;
                                    if (num12 != -1 && readOnlySpan.IsStartOfToken(num13))
                                    {
                                        num2 += 4.0;
                                        num11 = num13;
                                    }
                                    else
                                    {
                                        flag4 = false;
                                    }
                                }
                                else
                                {
                                    flag4 = false;
                                }
                            }

                            if (flag4)
                            {
                                num9++;
                            }

                            if (flag2 && !flag4 && num11 - num4 > 1)
                            {
                                if (flag5 || num9 == 0)
                                {
                                    return 0.0;
                                }

                                flag5 = true;
                            }

                            num4 = num11;
                            num3++;
                        }

                        if (num3 == 0)
                        {
                            return 0.0;
                        }

                        double num14 = 1.0 * num2 / (double)num3;
                        double num15 = 1.0 * (double)num5 / (double)readOnlySpan.Length;
                        if (flag2 && num8 > 0)
                        {
                            num15 = Math.Max(num15, 1.0 * (double)num9 / (double)num8);
                        }

                        if (flag2 && num14 <= 3.0)
                        {
                            return 0.0;
                        }

                        if (flag)
                        {
                            num14 *= 1.5;
                        }

                        return num14 + num14 * num15;
                    }
            }
        }
        private VSCodeWorkspacesSearchResult CreateWorkspaceResult(VSCodeWorkspace ws, string searchedText)
        {
            var title = $"{ws.FolderName}";
            var typeWorkspace = ws.WorkspaceTypeToString();

            if (ws.TypeWorkspace != TypeWorkspace.Local)
            {
                title = $"{title}{(ws.ExtraInfo != null ? $" - {ws.ExtraInfo}" : string.Empty)} ({typeWorkspace})";
            }
            
            var tooltip = $"{Resources.Workspace}{(ws.TypeWorkspace != TypeWorkspace.Local ? $" {Resources.In} {typeWorkspace}" : string.Empty)}: {SystemPath.RealPath(ws.RelativePath)}";

            return new VSCodeWorkspacesSearchResult(title, tooltip, new ProcessStartInfo
            {
                FileName = ws.VSCodeInstance.ExecutablePath,
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                Arguments= $"--folder-uri {ws.Path}"
            },SearchAppName,title, searchedText,"VSCode Workspace",getscore(title,searchedText), _supportedOperations);
            }



        public IAsyncEnumerable<ISearchResult> SearchAsync(SearchRequest searchRequest, CancellationToken cancellationToken)

        {
            var local = _workspacesApi.Workspaces.ToList();
            var remote = _machinesApi.Machines.ToList();
            IEnumerable<ISearchResult> res()
            {
                if (cancellationToken.IsCancellationRequested || searchRequest.SearchType == SearchType.SearchProcess)
                    yield break;
                string searchedTag = searchRequest.SearchedTag;
                string searchedText = searchRequest.SearchedText;


                var results = new List<ISearchResult>();

                // User defined extra workspaces
                //if (defaultInstalce != null)
                //{
                //    workspaces.AddRange(_settings.CustomWorkspaces.Select(uri => VSCodeWorkspacesApi.ParseVSCodeUri(uri, defaultInstalce)));
                //}

                // Search opened workspaces
                //if (_settings.DiscoverWorkspaces)
                //{
                //    workspaces.AddRange(_workspacesApi.Workspaces);
                //}

                // Simple de-duplication
                //results.AddRange(_workspacesApi.Workspaces
                //    .Select(i => { return CreateWorkspaceResult(i, searchedText); })
                // );
                foreach (var item in local)
                {
                    yield return CreateWorkspaceResult(item, searchedText);
                }
                // Search opened remote machines
                //if (_settings.DiscoverMachines)
                //{
                foreach (var a in remote)    
                {
                    var title = $"{a.Host}";

                    if (!string.IsNullOrEmpty(a.User) && !string.IsNullOrEmpty(a.HostName))
                    {
                        title += $" [{a.User}@{a.HostName}]";
                    }


                    yield return new VSCodeWorkspacesSearchResult(title, Resources.SSHRemoteMachine, new ProcessStartInfo
                    {
                        FileName = a.VSCodeInstance.ExecutablePath,
                        UseShellExecute = true,
                        Arguments = $"--new-window --enable-proposed-api ms-vscode-remote.remote-ssh --remote ssh-remote+{((char)34) + a.Host + ((char)34)}",
                        WindowStyle = ProcessWindowStyle.Hidden,
                    }, SearchAppName, title, searchedText, "VSCode Workspace", getscore(title, searchedText), _supportedOperations);
                };
             
                //foreach (var i in results)
                //{
                //    //if (i.Score>0)
                //    //{
                //    yield return i;
                //    //}
                //}
            }
            return new SynchronousAsyncEnumerable(res());

        }
        public ValueTask<IHandleResult> HandleSearchResult(ISearchResult searchResult)
        {
            if (!(searchResult is VSCodeWorkspacesSearchResult result))
            {
                throw new InvalidCastException(nameof(VSCodeWorkspacesSearchResult));
            }


            // Get Fluent Search process manager instance
            //IProcessManager managerInstance = ProcessUtils.GetManagerInstance();
            //switch (conversionSearchOperation.ConversionType)
            //{
            //    case ConversionType.Hex:
            //        managerInstance.StartNewProcess(
            //            $"https://www.hexadecimaldictionary.com/hexadecimal/{numberConversionSearchResult.Number:X}");
            //        break;
            //    case ConversionType.Binary:
            //        managerInstance.StartNewProcess(
            //            $"https://www.binary-code.org/binary/16bit/{Convert.ToString(numberConversionSearchResult.Number, 2)}");
            //        break;
            //    default:
            //        throw new ArgumentOutOfRangeException();
            //}
            try
            {
                Process.Start(result.Process);
                return new ValueTask<IHandleResult>(new HandleResult(true, false));
            }
            catch (Exception)
            {
                return new ValueTask<IHandleResult>(new HandleResult(false, false));
            }
            

            
        }
    }
}
