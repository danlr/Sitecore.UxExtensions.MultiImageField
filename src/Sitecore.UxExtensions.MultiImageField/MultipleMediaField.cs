namespace Sitecore.UxExtensions.MultiImageField
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Web.UI;

    using Data;
    using Data.Items;
    using Diagnostics;
    using Exceptions;
    using Globalization;
    using Resources;
    using Resources.Media;
    using Shell.Applications.ContentEditor;
    using Shell.Applications.Dialogs.MediaBrowser;
    using Sitecore;
    using Web.UI;
    using Web.UI.Sheer;

    using Control = Web.UI.HtmlControls.Control;

    public class MultipleMediaField : Control, IContentField
    {
        public string ItemID
        {
            get => StringUtil.GetString(this.ViewState[nameof(this.ItemID)]);
            set
            {
                Assert.ArgumentNotNull(value, nameof(value));
                this.ViewState[nameof(this.ItemID)] = value;
            }
        }

        public string ItemLanguage
        {
            get
            {
                return StringUtil.GetString(this.ViewState[nameof(this.ItemLanguage)]);
            }
            set
            {
                Assert.ArgumentNotNull(value, nameof(value));
                this.ViewState[nameof(this.ItemLanguage)] = value;
            }
        }

        public string Source
        {
            get => StringUtil.GetString(this.GetViewStateProperty(nameof(this.Source), string.Empty));
            set
            {
                if (value == this.Value)
                {
                    return;
                }

                this.SetViewStateProperty(nameof(this.Source), value, string.Empty);
            }
        }

        public string FieldID
        {
            get => StringUtil.GetString(this.ViewState[nameof(this.FieldID)]);
            set
            {
                Assert.ArgumentNotNull(value, nameof(value));
                this.ViewState[nameof(this.FieldID)] = value;
            }
        }

        public string GetValue()
        {
            return this.Value;
        }

        public void SetValue(string value)
        {
            this.Value = value;
        }

        public virtual void AddValue(string id)
        {
            this.Value = string.IsNullOrWhiteSpace(this.Value) ? id : this.Value + "|" + id;
        }

        public override void HandleMessage(Message message)
        {
            Assert.ArgumentNotNull(message, "message");
            base.HandleMessage(message);

            if (message["id"] != this.ID)
            {
                return;
            }

            switch (message.Name)
            {
                case "imagelist:add":
                    Sitecore.Context.ClientPage.Start(this, "AddItem");
                    break;
                case "imagelist:clear":
                    Sitecore.Context.ClientPage.Start(this, "RemoveAll");
                    break;
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            Assert.ArgumentNotNull(e, nameof(e));
            base.OnLoad(e);
            this.UpdateValueFromClient();
        }

        protected void SetModified()
        {
            Sitecore.Context.ClientPage.Modified = true;
        }

        protected virtual void AddItem(ClientPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            if (args.IsPostBack)
            {
                if (string.IsNullOrEmpty(args.Result) || args.Result == "undefined")
                {
                    return;
                }

                MediaItem mediaItem = Client.ContentDatabase.Items[args.Result];
                if (mediaItem != null)
                {
                    this.AddValue(mediaItem.ID.ToString());
                    this.UpdateValueToClient();
                }
                else
                {
                    SheerResponse.Alert("Item not found.");
                }
            }
            else
            {
                this.UpdateValueFromClient();
                this.ShowBrowseImageDialog();
                args.WaitForPostBack();
            }
        }

        protected void RemoveAll(ClientPipelineArgs args)
        {
            this.SetValue(string.Empty);
            this.UpdateValueToClient();
        }

        protected virtual void ShowBrowseImageDialog()
        {
            string source = StringUtil.GetString(this.Source, "/sitecore/media library");
            Language language = Language.Parse(this.ItemLanguage);
            MediaBrowserOptions mediaBrowserOptions = new MediaBrowserOptions();
            Item root = Client.ContentDatabase.GetItem(source, language);

            mediaBrowserOptions.Root = root ?? throw new ClientAlertException("The source of this field points to an item that does not exist.");

            SheerResponse.ShowModalDialog(mediaBrowserOptions.ToUrlString().ToString(), "1200px", "700px", string.Empty, true);
        }

        protected virtual void UpdateValueToClient()
        {
            IEnumerable<ID> ids = Sitecore.Data.ID.ParseArray(this.Value);
            this.SetModified();
            StringBuilder sb = new StringBuilder();
            foreach (ID id in ids)
            {
                this.RenderItem(id, sb);
            }

            Sitecore.Context.ClientPage.ClientResponse.SetAttribute(this.ID + "_Value", "value", this.Value);
            Sitecore.Context.ClientPage.ClientResponse.SetInnerHtml(this.ID + "_selected", sb.ToString());
            Sitecore.Context.ClientPage.ClientResponse.Eval("MultipleMediaField.reload('" + this.ID + "')");
        }

        private void UpdateValueFromClient()
        {
            string str = Sitecore.Context.ClientPage.ClientRequest.Form[this.ID + "_Value"];
            if (str == null || this.Value == str)
            {
                return;
            }

            this.SetModified();
            this.Value = str;
        }

        protected override void DoRender(HtmlTextWriter output)
        {
            this.RenderInitJs(output);

            this.ServerProperties["ID"] = this.ID;
            string str = string.Empty;

            output.Write("<input id=\"" + this.ID + "_Value\" type=\"hidden\" value=\"" + StringUtil.EscapeQuote(this.Value) + "\" />");
            output.Write("<div class='scContentControlMultilistContainer'>");
            output.Write("<table" + this.GetControlAttributes() + "style=\"min-height:182px;\" >");
            output.Write("<colgroup><col style=\"width:1%\"/><col/></colgroup>");
            output.Write("<tr>");
            output.Write("<td valign=\"top\">");
            this.RenderButton(output, Themes.MapTheme("Office/16x16/delete.png"), "javascript:MultipleMediaField.deleteCurrent('" + this.ID + "')");
            output.Write("<br />");
            this.RenderButton(output, Themes.MapTheme("Office/16x16/navigate_up.png"), "javascript:MultipleMediaField.moveUp('" + this.ID + "')");
            output.Write("<br />");
            this.RenderButton(output, Themes.MapTheme("Office/16x16/navigate_down.png"), "javascript:MultipleMediaField.moveDown('" + this.ID + "')");
            output.Write("</td>");
            output.Write("<td valign=\"top\" height=\"100%\">");
            output.Write("<select id=\"" + this.ID + "_selected\" class=\"scContentControlMultilistBox image-picker\" size=\"10\"" + str + " ondblclick=\"javascript:MultipleMediaField.openCurrent('" + this.ID + "')\" >");
            IEnumerable<ID> selectedIdsFromString = Data.ID.ParseArray(this.Value);
            StringBuilder sb = new StringBuilder();

            foreach (ID id in selectedIdsFromString)
            {
                this.RenderItem(id, sb);
            }

            output.Write(sb);
            output.Write("</select>");
            output.Write("</td>");
            output.Write("</tr>");
            output.Write("</table>");
            output.Write("<script>setTimeout(function(){ $sc(\"#" + this.ID + "_selected\").imagepicker();}, 200);</script>");
            output.Write("</div>");
        }

        protected virtual void RenderInitJs(HtmlTextWriter output)
        {
            output.Write("<script type='text/javascript'>\r\n" +
                         "          (function(){\r\n" +
                         "              if (!document.getElementById('imagePicker')) {\r\n" +
                         "                var head = document.getElementsByTagName('head')[0];\r\n" +
                         "                head.appendChild(new Element('script', { type: 'text/javascript', src: '/sitecore/shell/Applications/Multiple Media Field/image-picker/image-picker.js', id: 'imagePicker', async: 'false', defer: 'false' }));\r\n" +
                         "                head.appendChild(new Element('link', { type: 'text/css', rel:'stylesheet', href: '/sitecore/shell/Applications/Multiple Media Field/image-picker/image-picker.css' }));\r\n" +
                         "                head.appendChild(new Element('script', { type: 'text/javascript', src: '/sitecore/shell/Applications/Multiple Media Field/MultipleMediaField.js', async: 'false', defer: 'false' }));\r\n" +
                         "              }\r\n" +
                         "          }());</script>");
        }

        protected virtual void RenderItem(ID id, StringBuilder sb)
        {
            Item item;
            using (new LanguageSwitcher(this.ItemLanguage))
            {
                item = Sitecore.Context.ContentDatabase.GetItem(id);
            }

            if (item != null)
            {
                MediaItem media = item;
                string imageUrl;

                if (media.Size > 0)
                {
                    imageUrl = MediaManager.GetThumbnailUrl(item);
                }
                else
                {
                    imageUrl = Images.GetThemedImageSource(item.Appearance.Icon, ImageDimension.id48x48);
                }

                sb.AppendFormat("<option data-item-path=\"{3}\" data-img-src=\"{2}\" value=\"{0}\" data-img-label=\"{1}\">{1}</option>", item.ID, item.DisplayName, imageUrl, item.Paths.MediaPath);
            }
            else
            {
                sb.AppendFormat("<option value=\"{0}\">{0} {1}</option>", id, Translate.Text("[Item not found]"));
            }
        }

        protected virtual void RenderButton(HtmlTextWriter output, string icon, string click)
        {
            Assert.ArgumentNotNull(output, nameof(output));
            Assert.ArgumentNotNull(icon, nameof(icon));
            Assert.ArgumentNotNull(click, nameof(click));
            ImageBuilder imageBuilder = new ImageBuilder
            {
                Src = icon,
                Class = "scNavButton",
                Width = 16,
                Height = 16,
                Margin = "2px",
                OnClick = click
            };

            output.Write(imageBuilder.ToString());
        }
    }
}