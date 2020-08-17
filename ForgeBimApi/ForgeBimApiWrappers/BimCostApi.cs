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

using Autodesk.Forge.BIM360.Serialization;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;

namespace Autodesk.Forge.BIM360
{
    public class BimCostApi: ForgeApi
    {

        private string m_ThreeLeggedToken = null;

        public BimCostApi(Token token, ApplicationOptions options) : base(token, options)
        {            
            ContentType = "application/json";
        }

        public String ThreeLeggedToken
        {
            get
            {
                return m_ThreeLeggedToken;
            }
            set
            {
                m_ThreeLeggedToken = value;
            }


        }


        public IRestResponse GetBudgetCodeTemplates(string costContainerId, out List<CostTemplate> result)
        {
            Log.Info($"Querying Cost Template from project '{options.ForgeBimAccountId}'");
            result = new List<CostTemplate>();
            IRestResponse response = null;
            try
            {
                var request = new RestRequest(Method.GET);
                //request.Resource = "cost/v1/containers/{ContainerId}/templates";
                request.Resource = Urls["cost_templates"];
                request.AddParameter("ContainerId", costContainerId, ParameterType.UrlSegment);
                request.AddHeader("authorization", $"Bearer {ThreeLeggedToken}");

                response = ExecuteRequest(request);

                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    JsonSerializerSettings settings = new JsonSerializerSettings();
                    settings.NullValueHandling = NullValueHandling.Ignore;
                    List<CostTemplate> templates = JsonConvert.DeserializeObject<List<CostTemplate>>(response.Content, settings);
                    result.AddRange(templates);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                throw ex;
            }

            return response;
        }

       
        /// <summary>
        /// 
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        public IRestResponse GetBudgetCodeSegments(string costContainerId, string templateId, out List<CostSegment> result)
        {
            int limit = 100;
            Log.Info($"Querying Cost Budget Segments from project '{options.ForgeBimAccountId}'");
            result = new List<CostSegment>();
            List<CostSegment> segments;
            IRestResponse response = null;
            int offset = 0;
            do
            {
                segments = null;
                try
                {
                    var request = new RestRequest(Method.GET);
                    //request.Resource = "cost/v1/containers/{ContainerId}/templates/{TemplateId}/segments";
                    request.Resource = Urls["cost_segments"];
                    //TBD: change to container id
                    request.AddParameter("ContainerId", costContainerId, ParameterType.UrlSegment);
                    request.AddParameter("TemplateId", templateId, ParameterType.UrlSegment);

                    request.AddHeader("authorization", $"Bearer {ThreeLeggedToken}");
                    request.AddParameter("limit", limit, ParameterType.QueryString);
                    request.AddParameter("offset", offset, ParameterType.QueryString);

                    response = ExecuteRequest(request);

                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        JsonSerializerSettings settings = new JsonSerializerSettings();
                        settings.NullValueHandling = NullValueHandling.Ignore;
                        segments = JsonConvert.DeserializeObject<List<CostSegment>>(response.Content, settings);
                        result.AddRange(segments);
                        offset += limit;
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex);
                    throw ex;
                }
            }
            while (segments != null && segments.Count == limit);

            return response;

        }

        public IRestResponse GetBudgetCodeSegment(string costContainerId, string templateId, string segmentId)
        {
            Log.Info($"Querying Projects from AccountID '{options.ForgeBimAccountId}'");
            IRestResponse response = null;
            try
            {
                var request = new RestRequest(Method.GET);
                //request.Resource = "cost/v1/containers/{ContainerId}/templates/{TemplateId}/segments/{SegmentId}";
                request.Resource = Urls["cost_segments_segmentId"];
                request.AddParameter("ContainerId", costContainerId, ParameterType.UrlSegment);
                request.AddParameter("TemplateId", templateId, ParameterType.UrlSegment);
                request.AddParameter("SegmentId", segmentId, ParameterType.UrlSegment);
                request.AddHeader("authorization", $"Bearer {ThreeLeggedToken}");

                response = ExecuteRequest(request);
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                throw ex;
            }

            return response;
        }

        public IRestResponse PostBudgetCodeSegment(string costContainerId, string templateId, CostSegment segment)
        {
            if( costContainerId == null || templateId == null || segment == null)
            {
                Log.Error("The input paramter is null");
                return null;
            }

            var request = new RestRequest(Method.POST);
            //request.Resource = "hq/v1/accounts/{AccountId}/projects";
            request.Resource = Urls["cost_segments"];
            request.AddParameter("ContainerId", costContainerId, ParameterType.UrlSegment);
            request.AddParameter("TemplateId", templateId, ParameterType.UrlSegment);

            JsonSerializerSettings settings = new JsonSerializerSettings();
            settings.DateFormatString = "yyyy-MM-dd";
            settings.NullValueHandling = NullValueHandling.Ignore;
            string segmentString = JsonConvert.SerializeObject(segment, settings);
            request.AddParameter("application/json", segmentString, ParameterType.RequestBody);

            request.AddHeader("content-type", ContentType);
            request.AddHeader("authorization", $"Bearer {ThreeLeggedToken}");

            IRestResponse response = ExecuteRequest(request);
            return response;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="costContainerId"></param>
        /// <param name="templateId"></param>
        /// <param name="segment"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public IRestResponse GetBudgetCodeSegmentValues(string costContainerId, string templateId, CostSegment segment, out List<CostSegmentValue> result)
        {
            int limit = 100;
            Log.Info($"Querying Cost Budget Segments from project '{options.ForgeBimAccountId}'");
            result = new List<CostSegmentValue>();
            List<CostSegmentValue> segmentValues;
            IRestResponse response = null;
            int offset = 0;
            do
            {
                segmentValues = null;
                try
                {
                    var request = new RestRequest(Method.GET);
                    //request.Resource = "cost/v1/containers/{ContainerId}/templates/{TemplateId}/segments/{SegmentId}/values";
                    request.Resource = Urls["cost_segments_segmentId_values"];
                    //TBD: change to container id
                    request.AddParameter("ContainerId", costContainerId, ParameterType.UrlSegment);
                    request.AddParameter("TemplateId", templateId, ParameterType.UrlSegment);
                    request.AddParameter("SegmentId", segment, ParameterType.UrlSegment);

                    request.AddHeader("authorization", $"Bearer {ThreeLeggedToken}");
                    request.AddParameter("limit", limit, ParameterType.QueryString);
                    request.AddParameter("offset", offset, ParameterType.QueryString);

                    response = ExecuteRequest(request);

                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        JsonSerializerSettings settings = new JsonSerializerSettings();
                        settings.NullValueHandling = NullValueHandling.Ignore;
                        segmentValues = JsonConvert.DeserializeObject<List<CostSegmentValue>>(response.Content, settings);
                        result.AddRange(segmentValues);
                        offset += limit;
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex);
                    throw ex;
                }
            }
            while (segmentValues != null && segmentValues.Count == limit);

            return response;

        }

        public IRestResponse PostBudgetCodeSegmentValue(string costContainerId, string templateId, string segmentId, CostSegmentValue segmentValue)
        {
            var request = new RestRequest(Method.POST);
            //request.Resource = "cost/v1/containers/{ContainerId}/templates/{TemplateId}/segments/{SegmentId}/values";
            request.Resource = Urls["cost_segment_values"];
            request.AddParameter("ContainerId", costContainerId, ParameterType.UrlSegment);
            request.AddParameter("TemplateId", templateId, ParameterType.UrlSegment);
            request.AddParameter("SegmentId", segmentId, ParameterType.UrlSegment);

            JsonSerializerSettings settings = new JsonSerializerSettings();
            settings.DateFormatString = "yyyy-MM-dd";
            settings.NullValueHandling = NullValueHandling.Ignore;
            string segmentValueString = JsonConvert.SerializeObject(segmentValue, settings);
            request.AddParameter("application/json", segmentValueString, ParameterType.RequestBody);

            request.AddHeader("content-type", ContentType);
            request.AddHeader("authorization", $"Bearer {ThreeLeggedToken}");

            IRestResponse response = ExecuteRequest(request);
            return response;
        }

        public IRestResponse PostBudgetCodeSegmentValuesImport(string costContainerId, string templateId, string segmentId, List<CostSegmentValue> segmentValues)
        {
            var request = new RestRequest(Method.POST);
            //request.Resource = "cost/v1/containers/{ContainerId}/templates/{TemplateId}/segments/{SegmentId}/values:import";
            request.Resource = Urls["cost_segments_segmentId_values_import"];
            request.AddParameter("ContainerId", costContainerId, ParameterType.UrlSegment);
            request.AddParameter("TemplateId", templateId, ParameterType.UrlSegment);
            request.AddParameter("SegmentId", segmentId, ParameterType.UrlSegment);

            JsonSerializerSettings settings = new JsonSerializerSettings();
            settings.DateFormatString = "yyyy-MM-dd";
            settings.NullValueHandling = NullValueHandling.Ignore;
            string segmentValueString = JsonConvert.SerializeObject(segmentValues, settings);
            request.AddParameter("application/json", segmentValueString, ParameterType.RequestBody);

            request.AddHeader("content-type", ContentType);
            request.AddHeader("authorization", $"Bearer {ThreeLeggedToken}");

            IRestResponse response = ExecuteRequest(request);
            return response;
        }

        public IRestResponse GetBudgetCodeSegmentValue(string costContainerId, string templateId, string segmentId, string valueId)
        {
            Log.Info($"Querying Projects from AccountID '{options.ForgeBimAccountId}'");
            IRestResponse response = null;
            try
            {
                var request = new RestRequest(Method.GET);
                //request.Resource = "cost/v1/containers/{ContainerId}/templates/{TemplateId}/segments/{SegmentId}/values/{ValueId}";
                request.Resource = Urls["cost_segments_values_valueId"];
                request.AddParameter("ContainerId", costContainerId, ParameterType.UrlSegment);
                request.AddParameter("TemplateId", templateId, ParameterType.UrlSegment);
                request.AddParameter("SegmentId", segmentId, ParameterType.UrlSegment);
                request.AddParameter("ValueId", valueId, ParameterType.UrlSegment);
                request.AddHeader("authorization", $"Bearer {ThreeLeggedToken}");

                response = ExecuteRequest(request);
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                throw ex;
            }

            return response;
        }

    }
}
