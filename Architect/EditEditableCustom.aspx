<%@ Page Title="" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true" CodeBehind="EditEditableCustom.aspx.cs" Inherits="_min.Architect.EditEditableCustom"
 EnableEventValidation="false" %>
<%@ MasterType  virtualPath="~/Site.master"%>

<%@ Register assembly="Webmin" namespace="_min.Controls" tagprefix="cc1" %>

<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
        <script src="/Scripts/globalUI.js?<%=DateTime.Now.Ticks.ToString()%>" type="text/javascript"></script>
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
<asp:ScriptManager ID="ScriptManager1" runat="server">
    </asp:ScriptManager>
    <asp:Panel ID="MainPanel" runat="server">
        <asp:Label ID="Label4" runat="server" Text=" "></asp:Label>
        <asp:Button ID="saveButton" runat="server" onclick="SaveButton_Click" 
            Text="Save" />
        <asp:LinkButton ID="backButton" runat="server">Back</asp:LinkButton>
        <asp:BulletedList ID="validationResult" runat="server">
        </asp:BulletedList>
    
    </asp:Panel>
    
</asp:Content>