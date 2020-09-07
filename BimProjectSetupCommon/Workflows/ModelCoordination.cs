/////////////////////////////////////////////////////////////////////
// Copyright (c) Autodesk, Inc. All rights reserved
// Written by Forge Partner Development
//
// Permission to use, copy, modify, and distribute this software in
// object code form for any purpose and without fee is hereby granted,
// provided that the above copyright notice appears in all copies and
// that both that copyright notice and the limited warranty and
// restricted rights notice below appear in all supporting
// documentation.
//
// AUTODESK PROVIDES THIS PROGRAM 'AS IS' AND WITH ALL FAULTS.
// AUTODESK SPECIFICALLY DISCLAIMS ANY IMPLIED WARRANTY OF
// MERCHANTABILITY OR FITNESS FOR A PARTICULAR USE.  AUTODESK, INC.
// DOES NOT WARRANT THAT THE OPERATION OF THE PROGRAM WILL BE
// UNINTERRUPTED OR ERROR FREE.
/////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using Autodesk.Forge.BIM360;
using Autodesk.Forge.BIM360.Serialization;
using Autodesk.Forge.Bim360.ModelCoordination.ModelSet;
using BimProjectSetupCommon.Helpers;
using RestSharp;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Threading;

namespace BimProjectSetupCommon.Workflow
{

    public class ModelSetEx : NewModelSet
    {
        [JsonProperty("projectName")]
        public string projectName { get; set; }
    }



    public class ModelCoordinationWorkflow : ThreeLeggedWorkflow
    {
        private ModelSetClient       _modelSetClient = null;
        private BimProjectFoldersApi _foldersApi     = null;
        private HubsApi              _hubsApi        = null;
        private List<ModelSetEx>     _modelSets      = null;

        public ModelCoordinationWorkflow(AppOptions options ) : base(options)
        {
            DataController.InitializeDmProjects();
        }


        /// <summary>
        /// 
        /// </summary>
        public void prepareData()
        {
            string threeLeggedToken = GetToken();
            _modelSetClient = new ModelSetClient(new System.Net.Http.HttpClient
            {
                BaseAddress = new Uri("https://developer.api.autodesk.com/bim360/modelset/"),
                DefaultRequestHeaders =
                    {
                        Authorization = new AuthenticationHeaderValue("Bearer", threeLeggedToken)
                    }
            });

            _foldersApi = new BimProjectFoldersApi(GetToken, _options);
            _hubsApi = new HubsApi(GetToken, _options);

        }


        /// <summary>
        /// Main method to setup Model Sets from excel file
        /// </summary>
        public void SetupModelSetsFromCsvProcess()
        {
            try
            {
                DataController._modelSetTable = CsvReader.ReadDataFromCSV( DataController._modelSetTable, DataController._options.ModelSetFilePath );
                Log.Info($"Read data from CSV file at {DataController._options.ModelSetFilePath}");
                if ( false == _options.TrialRun)
                {
                    _modelSets = GetModelSetsFromTable( DataController._modelSetTable);
                    SetupModelSets(_modelSets);
                }
                else
                {
                    Log.Info("Trial run finished. No further processing");
                }
            }
            catch ( Exception ex)
            {
                Log.Error(ex);
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="modelSetTable"></param>
        /// <returns></returns>
        protected List<ModelSetEx> GetModelSetsFromTable( DataTable modelSetTable )
        {
            if( modelSetTable == null )
                return null;

            // sort data table by project_name
            DataView view = modelSetTable.DefaultView;
            view.Sort = "project_name desc";
            DataTable sorted = view.ToTable();

            List<ModelSetEx> modelSets = new List<ModelSetEx>();
            int i = 0;

            Log.Info($"Start to read {sorted.Rows.Count}  model set");
            // Validate the data and convert
            foreach (DataRow row in sorted.Rows)
            {
                i++;
                string projectName = Util.GetStringOrNull(row["project_name"]);

                if (string.IsNullOrWhiteSpace(projectName))
                {
                    Log.Warn($"No project name provided for row {i} - skipping this line!");
                    continue;
                }

                var modelSet = GetModelSetForRow(row);
                if (modelSet != null)
                {
                    Log.Info($"Read model set + {modelSet.Name}");
                    modelSets.Add(modelSet);
                }
            }
            return modelSets;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="row"></param>
        /// <returns></returns>
        private ModelSetEx GetModelSetForRow(DataRow row)
        {
            ModelSetEx modelSet = new ModelSetEx();
            modelSet.projectName = Util.GetStringOrNull(row["project_name"]);
            modelSet.Name = Util.GetStringOrNull(row["name"]);
            modelSet.Description = Util.GetStringOrNull(row["description"]);
            modelSet.IsDisabled = bool.Parse( Util.GetStringOrNull(row["isDisabled"]) );
            string folderName = Util.GetStringOrNull(row["folder"]);
            string folderUrn = GetFolderUrnFromName( modelSet.projectName, folderName);

            modelSet.Folders = new List<ModelSetFolder>();
            ModelSetFolder folder = new ModelSetFolder();
            folder.FolderUrn = folderUrn;
            modelSet.Folders.Add(folder);

            return modelSet;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="projectName"></param>
        /// <param name="folderName"></param>
        /// <returns></returns>
        private string GetFolderUrnFromName( string projectName, string folderName)
        {
            if( folderName == null )
                return null;

            string[] items = folderName.Split('/');

            var dmProject = DataController.DmProjects.FirstOrDefault(p => p.attributes != null && p.attributes.name != null && p.attributes.name.Equals(projectName, StringComparison.InvariantCultureIgnoreCase));
            if (dmProject == null || dmProject.id == null )
                return null;

            IList<Folder> folders = null;
            IRestResponse response = _hubsApi.GetTopFolders(dmProject.id);
            JsonSerializerSettings jss = new JsonSerializerSettings();
            jss.NullValueHandling = NullValueHandling.Ignore;
            folders = JsonConvert.DeserializeObject<JsonApiResponse<IList<Folder>>>(response.Content, jss).data;

            Folder folder = null;
            for ( int i = 0; i< items.Length; ++i)
            {
                folder = folders.FirstOrDefault(f => f.attributes!= null && f.attributes.displayName !=null && f.attributes.displayName.Equals(items[i], StringComparison.InvariantCultureIgnoreCase));
               if( folder == null)
                {
                    Log.Warn($"Can not find folder: {items[i]}");
                    break;
                }
                folders = _foldersApi.GetSubFolders(dmProject.id, folder.id);
            }
            return (folder!=null)? folder.id : null;
        }

        /// <summary>
        /// Create model sets
        /// </summary>
        /// <param name="modelSets"></param>
        public void SetupModelSets( List<ModelSetEx> modelSets)
        {
            if(modelSets == null || modelSets.Count == 0)
            {
                Log.Info("segment list is null or no segment in the list");
                return;
            }

            Log.Info($"");
            Log.Info($"Start creating modelSets to cost budget template...");
            var distinctSegments = modelSets.Select(modelSet => modelSet.Name).Distinct();
            foreach (ModelSetEx modelSet in modelSets)
            {
                var dmProject = DataController.DmProjects.FirstOrDefault(p => p.attributes != null && p.attributes.name != null && p.attributes.name.Equals(modelSet.projectName, StringComparison.InvariantCultureIgnoreCase));

                if( dmProject == null )
                {
                    Log.Error("Can not get the project.");
                    return;
                }

                // the model coordination container id is same as project id
                string costContainerId = dmProject.id.StartsWith("b.")? dmProject.id.Remove(0, 2):dmProject.id;
                try
                {
                    Log.Info($"- add model set: { modelSet.Name} to project ");
                    ModelSetJob job = _modelSetClient.CreateModelSetAsync(new Guid(costContainerId), modelSet).Result;
                    while( job.Status == ModelSetJobStatus.Running )
                    {
                        Thread.Sleep(1000);
                        job = _modelSetClient.GetModelSetJobAsync(new Guid(costContainerId), job.ModelSetId, job.JobId).Result;
                    }
                    if(job.Status == ModelSetJobStatus.Succeeded)
                    {
                        Log.Info($"Model Set {modelSet.Name} is created");
                    }else
                    {
                        Log.Error($"Model Set {modelSet.Name} failed to be created");
                    }
                }
                catch ( Exception ex)
                {
                    Log.Error($"ModelSet {modelSet.Name} failed to be created due to {ex.Message}");
                }
            }
        }
    }
}
