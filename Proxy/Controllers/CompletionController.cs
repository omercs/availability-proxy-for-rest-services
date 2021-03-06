//-------------------------------------------------------------------------------------------------
// <copyright company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
//
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//      http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, 
// EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR 
// CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT.
//
// See the Apache Version 2.0 License for specific language governing 
// permissions and limitations under the License.
// </copyright>
//
// <summary>
// 
//
//     
// </summary>
//-------------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ReverseProxy.Models;
using System.Net;
using Microsoft.WindowsAzure.ServiceRuntime;
using ReverseProxy;
using System.Text;

namespace ReverseProxy.Controllers
{
    [NoCache]
    [HandleError]
    public class CompletionController : Controller
    {
        //
        // GET: /Completion/

        public ActionResult Index(string service)
        {
            ViewBag.Location = RoleEnvironment.GetConfigurationSettingValue("Name");
            ViewBag.Service = service;

            AdaptiveStateMachine<SerialilzableWebRequest, HttpStatusCode> stateMachine;
            if (!Global.stateMachines.TryGetValue(service, out stateMachine) || stateMachine == null)
            {
                return View("ServiceNotPresent");
            }

            var outcomes = new List<CompletionModel>();
            foreach (var i in stateMachine.Paxos.Outcomes.Keys.OrderByDescending(i => i))
            {
                var results = stateMachine.Paxos.Outcomes[i];
                var values = results.Values.Distinct();
                var conflict = values.Count() > 1;
                var sb = new StringBuilder();
                foreach (var v in values)
                {
                    var nodes = results.Where(kv => ((HttpStatusCode)kv.Value) == ((HttpStatusCode)v)).Select(kv => kv.Key);
                    sb.Append(nodes.Count());
                    sb.Append(' ');
                    sb.Append(v);
                    sb.Append(": ");
                    sb.Append(String.Join(",", nodes));
                    sb.Append("; ");
                }
                outcomes.Add(new CompletionModel { Instance = i, Message = sb.ToString(), Conflict = conflict });
                     }

            return View(outcomes);
        }

        //
        // GET: /Completion/Details/5

        public ActionResult Details(int id)
        {
            return View();
        }
    }
}
