<%@ Page Title="" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true" CodeBehind="EditMenu.aspx.cs" Inherits="_min.Architect.EditMenu" %>
<%@ Register assembly="Webmin" namespace="_min.Controls" tagprefix="cc1" %>
<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <asp:ScriptManager ID="ScriptManager1" runat="server">
    </asp:ScriptManager>
    <asp:Panel ID="MainPanel" runat="server">
        <cc1:TreeBuilderControl ID="tbc" runat="server" />
        <asp:Button ID="SaveButton" runat="server" Text="Save" 
            onclick="OnSaveButtonClicked" />
    </asp:Panel>
    
</asp:Content>