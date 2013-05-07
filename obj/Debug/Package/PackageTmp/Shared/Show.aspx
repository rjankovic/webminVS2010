<%@ Page Title="" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true" CodeBehind="Show.aspx.cs" EnableEventValidation="false" Inherits="_min.Shared.Show"%>
<%@ MasterType  virtualPath="~/Site.master"%>

<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
        <script src="/Scripts/globalUI.js?<%=DateTime.Now.Ticks.ToString()%>" type="text/javascript"></script>
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <asp:ScriptManager ID="ScriptManager1" runat="server">
    </asp:ScriptManager>
    <asp:Panel ID="MainPanel" runat="server">
    
    </asp:Panel>

</asp:Content>