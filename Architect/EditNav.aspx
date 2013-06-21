<%@ Page Title="" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true" CodeBehind="EditNav.aspx.cs" Inherits="_min.Architect.EditNav" 
    EnableEventValidation="false" %>
<%@ Register assembly="Webmin" namespace="_min.Controls" tagprefix="cc1" %>

<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
        <script src="/Scripts/globalUI.js?<%=DateTime.Now.Ticks.ToString()%>" type="text/javascript"></script>
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
<asp:ScriptManager ID="ScriptManager1" runat="server">
    </asp:ScriptManager>
    <asp:Panel ID="MainPanel" runat="server">
        <asp:Label ID="Label7" runat="server" Text="Panel name"></asp:Label>
        <asp:TextBox ID="PanelName" runat="server"></asp:TextBox>
        <asp:Label ID="Label4" runat="server" Text=" "></asp:Label>
        <cc1:M2NMappingControl ID="DisplayCols" runat="server" />
        
        <asp:RadioButtonList ID="NavControlType" runat="server">
        </asp:RadioButtonList>
        <asp:Label ID="UserActions" runat="server" 
            Text="Allowed user actions - delete does not apply for Navigation Trees"></asp:Label>
        <cc1:M2NMappingControl ID="actionsControl" runat="server" />
        <asp:Button ID="SaveButton" runat="server" onclick="SaveButton_Click" 
            Text="Save" />
        <asp:LinkButton ID="BackButton" runat="server">Back</asp:LinkButton>
        
        <asp:BulletedList ID="ValidationResult" runat="server">
        </asp:BulletedList>
    
    </asp:Panel>
    
</asp:Content>