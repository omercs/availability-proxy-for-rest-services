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
using Microsoft.WindowsAzure.ServiceRuntime;
using System.Net;

namespace ReverseProxy.Controllers
{
    [NoCache]
    [HandleError]
    public class StatusController : Controller
    {
        //
        // GET: /Status/

        public ActionResult Index(string service)
        {
            ViewBag.Service = service;
            ViewBag.Location = RoleEnvironment.GetConfigurationSettingValue("Name");

            AdaptiveStateMachine<SerialilzableWebRequest, HttpStatusCode> stateMachine;
            if (!Global.stateMachines.TryGetValue(service, out stateMachine) || stateMachine == null)
            {
                return View("ServiceNotPresent");
            }

            return View(GetStatus(stateMachine));
        }

        
        public ActionResult Refresh(string service)
        {
            AdaptiveStateMachine<SerialilzableWebRequest, HttpStatusCode> stateMachine;
            if (!Global.stateMachines.TryGetValue(service, out stateMachine))
            {
                return View("ServiceNotPresent");
            }

            return View(GetStatus(stateMachine));
        }

        private object GetStatus(AdaptiveStateMachine<SerialilzableWebRequest, HttpStatusCode> stateMachine)
        {
            var status = new List<StatusModel>();
            foreach (var l in stateMachine.EndpointNamesToUris.Keys.OrderBy(i => i))
            {
                DateTimeOffset last = DateTimeOffset.MinValue;
                stateMachine.LastContactByNode.TryGetValue(l, out last);
                var delay = l != stateMachine.Paxos.Self ? DateTimeOffset.Now - last : TimeSpan.Zero;
                var isLeader = stateMachine.Paxos.LeaderHint == l;
                var config = stateMachine.Paxos.ConfigurationHint;
                var active = !config.Learners.Except(config.Proposers).Contains(l);
                var color = ChooseColor(active, isLeader, delay);
                status.Add(new StatusModel { Location = l, LastContact = delay, Color = color });
            }
            return status;
        }

        public static string ChooseColor(bool active, bool leader, TimeSpan delay)
        {
            var l = "LightSkyBlue";
            var ag = "Green";
            var pg = "LightGreen";
            var ay = "Yellow";
            var py = "LightYellow";
            var ar = "Red";
            var pr = "Salmon";

            var _15sec = TimeSpan.FromSeconds(15);
            var _30sec = TimeSpan.FromSeconds(30);
            var _60sec = TimeSpan.FromSeconds(60);

            if (leader)
            {
                return l;
            }
            else if (active)
            {
                if (delay < _15sec)
                {
                    return ag;
                }
                else if (delay < _30sec)
                {
                    return ay;
                }
                else
                {
                    return ar;
                }
            }
            else
            {
                if (delay < _30sec)
                {
                    return pg;
                }
                else if (delay < _60sec)
                {
                    return py;
                }
                else
                {
                    return pr;
                }
            }
        }
    
        //
        // GET: /Status/Details/5

        public ActionResult Details(string service, int id)
        {
            return View(new StatusModel());
        }
    }
}
