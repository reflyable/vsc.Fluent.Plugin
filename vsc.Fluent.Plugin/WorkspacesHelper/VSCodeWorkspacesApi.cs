// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using vsc.Fluent.Plugin.VSCodeHelper;
using Microsoft.Data.Sqlite;

namespace vsc.Fluent.Plugin.WorkspacesHelper
{
    public class VSCodeWorkspacesApi
    {
        public VSCodeWorkspacesApi()
        {
        }

        public static VSCodeWorkspace ParseVSCodeUri(string uri, VSCodeInstance vscodeInstance)
        {
            if (uri != null && uri is string)
            {
                string unescapeUri = Uri.UnescapeDataString(uri);
                var typeWorkspace = WorkspacesHelper.ParseVSCodeUri.GetTypeWorkspace(unescapeUri);
                if (typeWorkspace.TypeWorkspace.HasValue)
                {
                    var folderName = Path.GetFileName(unescapeUri);

                    // Check we haven't returned '' if we have a path like C:\
                    if (string.IsNullOrEmpty(folderName))
                    {
                        DirectoryInfo dirInfo = new DirectoryInfo(unescapeUri);
                        folderName = dirInfo.Name.TrimEnd(':');
                    }

                    return new VSCodeWorkspace()
                    {
                        Path = uri,
                        RelativePath = typeWorkspace.Path,
                        FolderName = folderName,
                        ExtraInfo = typeWorkspace.MachineName,
                        TypeWorkspace = typeWorkspace.TypeWorkspace.Value,
                        VSCodeInstance = vscodeInstance,
                    };
                }
            }

            return null;
        }

        public List<VSCodeWorkspace> Workspaces
        {
            get
            {
                var results = new List<VSCodeWorkspace>();

                foreach (var vscodeInstance in VSCodeInstances.Instances)
                {
                    // storage.json contains opened Workspaces
                    var vscodeStorage = Path.Combine(vscodeInstance.AppData, "storage.json");

                    if (File.Exists(vscodeStorage))
                    {
                        var fileContent = File.ReadAllText(vscodeStorage);

                        try
                        {
                            var vscodeStorageFile = JsonSerializer.Deserialize<VSCodeStorageFile>(fileContent);

                            if (vscodeStorageFile != null)
                            {
                                // for previous versions of vscode
                                if (vscodeStorageFile.OpenedPathsList?.Workspaces3 != null)
                                {
                                    results.AddRange(
                                        vscodeStorageFile.OpenedPathsList.Workspaces3
                                            .Select(workspaceUri => ParseVSCodeUri(workspaceUri, vscodeInstance))
                                            .Where(uri => uri != null)
                                            .Select(uri => (VSCodeWorkspace)uri));
                                }

                                // vscode v1.55.0 or later
                                if (vscodeStorageFile.OpenedPathsList?.Entries != null)
                                {
                                    results.AddRange(vscodeStorageFile.OpenedPathsList.Entries
                                        .Select(x => x.FolderUri)
                                        .Select(workspaceUri => ParseVSCodeUri(workspaceUri, vscodeInstance))
                                        .Where(uri => uri != null));
                                }
                            }
                        }
                        catch (Exception)
                        {
                            //var message = $"Failed to deserialize ${vscodeStorage}";
                            //Main._context.API.LogException("VSCodeWorkspaceApi", message, ex);
                        }
                    }

                    // for vscode v1.64.0 or later
                    using var connection = new SqliteConnection($"Data Source={vscodeInstance.AppData}/User/globalStorage/state.vscdb;mode=readonly;cache=shared;");
                    connection.Open();
                    var command = connection.CreateCommand();
                    command.CommandText = "SELECT value FROM ItemTable where key = 'history.recentlyOpenedPathsList'";
                    var result = command.ExecuteScalar();
                    if (result != null)
                    {
                        using var historyDoc = JsonDocument.Parse(result.ToString()!);
                        var root = historyDoc.RootElement;
                        if (!root.TryGetProperty("entries", out var entries))
                            continue;
                        foreach (var entry in entries.EnumerateArray())
                        {
                            if (!entry.TryGetProperty("folderUri", out var folderUri))
                                continue;
                            var workspaceUri = folderUri.GetString();
                            var uri = ParseVSCodeUri(workspaceUri, vscodeInstance);
                            if (uri == null)
                                continue;
                            results.Add(uri);
                        }
                    }

                }

                return results;
            }
        }
    }
}
