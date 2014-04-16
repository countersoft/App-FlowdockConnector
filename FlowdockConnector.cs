using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using Countersoft.Foundation.Commons.Extensions;
using Countersoft.Gemini.Commons;
using Countersoft.Gemini.Commons.Dto;
using Countersoft.Gemini.Commons.Entity;
using Countersoft.Gemini.Contracts;
using Countersoft.Gemini.Extensibility.Apps;
using Countersoft.Gemini.Extensibility.Events;
using Countersoft.Gemini.Infrastructure;
using Countersoft.Gemini.Infrastructure.Apps;
using RestSharp;
using RestSharp.Serializers;

namespace FlowdockConnector
{
    public class AppConstants
    {
        public const string AppId = "C5DA3EF0-2B94-4FD3-81DB-2250E3894932";
    }

    public class FlowdockRoutes : IAppRoutes
    {
        public void RegisterRoutes(System.Web.Routing.RouteCollection routes)
        {
            routes.MapRoute(null, "apps/flowdock/configure", new { controller = "Flowdock", action = "SaveConfig" });
        }
    }

    [AppType(AppTypeEnum.Event),
    AppGuid("C5DA3EF0-2B94-4FD3-81DB-2250E3894932"),
    AppControlGuid("18F97E83-B3FA-4250-AC0C-49E6298DC76C"),
    AppAuthor("Countersoft"),
    AppKey("FlowDock"),
    AppName("Flowdock Connector"),
    AppDescription("Flowdock Connector"),
    AppRequiresConfigScreen(true)]
    [OutputCache(Duration = 0, NoStore = true, Location = System.Web.UI.OutputCacheLocation.None)]
    public class FlowdockController : BaseAppController
    {
        public ActionResult SaveConfig(string token)
        {
            GeminiContext.GlobalConfigurationWidgetStore.Save<string>(AppConstants.AppId, token);

            return JsonSuccess();
        }

        public override WidgetResult Caption(IssueDto issue)
        {
            return new WidgetResult() { Success = true, Markup = new WidgetMarkup("Flowdock") };
        }

        public override WidgetResult Show(IssueDto args = null)
        {
            var result = new WidgetResult();
            
            var data = GeminiContext.GlobalConfigurationWidgetStore.Get<string>(AppConstants.AppId);
            
            if (data == null)
            {
                data = new GlobalConfigurationWidgetData<string>();
                
                data.Value = string.Empty;
            }

            result.Success = true;
            
            result.Markup = new WidgetMarkup("views/settings.cshtml", data.Value);
            
            return result;
        }

        public override WidgetResult Configuration()
        {
            var result = new WidgetResult();
            
            var data = GeminiContext.GlobalConfigurationWidgetStore.Get<string>(AppConstants.AppId);
            
            if (data == null)
            {
                data = new GlobalConfigurationWidgetData<string>();
                
                data.Value = string.Empty;
            }

            result.Success = true;
            
            result.Markup = new WidgetMarkup("views/settings.cshtml", data.Value);
            
            return result;
        }
    }

    [AppType(AppTypeEnum.Event),
    AppGuid(AppConstants.AppId),
    AppName("Flowdock Connector"),
    AppDescription("Push item created/updated notifications into the Team Inbox")]
    public class FlowdockApp : AbstractIssueListener
    {
        public override void AfterCreateFull(IssueDtoEventArgs args)
        {
            Send(args);
        }

        public override void AfterUpdateFull(IssueDtoEventArgs args)
        {
            Send(args);
        }

        private bool Send(IssueDtoEventArgs args)
        {
            var token = args.Context.GlobalConfigurationWidgetStore.Get<string>(AppConstants.AppId);

            if (token == null || token.Value.IsEmpty()) return false;

            var client = new RestClient("https://api.flowdock.com/v1/messages");

            var request = new RestRequest("team_inbox/{token}", Method.POST);
            
            request.AddUrlSegment("token", token.Value);

            request.AddHeader("Accept", "application/json");
            
            request.JsonSerializer = new JsonSerializer();

            request.Method = Method.POST;
            
            request.RequestFormat = DataFormat.Json;
            
            request.AddHeader("Content-Type", "application/json");

            var data = new
                {
                    source = "Gemini", 
                    project = args.Issue.ProjectCode, 
                    from_name = args.User.Fullname, 
                    from_address = args.User.Email, 
                    subject = args.Issue.Title, 
                    content = args.Issue.Description,
                    tags = args.Issue.Type,
                    link = args.BuildIssueUrl(args.Issue)
                };
            
            //reply_to
            request.AddBody(data);

            var response = client.Execute(request);

            var content = response.Content;

            return true;
        }
    }

}
