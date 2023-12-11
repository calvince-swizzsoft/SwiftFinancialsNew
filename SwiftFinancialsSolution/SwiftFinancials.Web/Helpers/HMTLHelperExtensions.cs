using System;
using System.Linq;
using System.Web.Mvc;

namespace SwiftFinancials.Web
{
    public static class HMTLHelperExtensions
    {
        private static readonly string[] Administration = new string[] { "role", "membership" };

        private static readonly string[] Registry = new string[] { "Company","Customer" };

        public static string IsSelected(this HtmlHelper html, string controller = null, string action = null, string cssClass = null)
        {

            if (String.IsNullOrEmpty(cssClass))
                cssClass = "active";

            string currentAction = (string)html.ViewContext.RouteData.Values["action"];
            string currentController = (string)html.ViewContext.RouteData.Values["controller"];

            if (String.IsNullOrEmpty(controller))
                controller = currentController;

            if (String.IsNullOrEmpty(action))
                action = currentAction;

            if (controller == currentController && action == currentAction)
            {
                return cssClass;
            }
            else if (controller == "Administration" && Administration.Contains(currentController))
            {
                return cssClass;
            }
            else if (controller == "Registry" && Registry.Contains(currentController))
            {
                return cssClass;
            }
            else
            {
                return String.Empty;
            }
        }

        public static string PageClass(this HtmlHelper html)
        {
            string currentAction = (string)html.ViewContext.RouteData.Values["action"];
            return currentAction;
        }

    }
}