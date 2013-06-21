<%@ Page Title="" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true" CodeBehind="EditEditable.aspx.cs" Inherits="_min.Architect.EditEditable"
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
        <asp:Label ID="Label7" runat="server" Text="Panel name"></asp:Label>
        <asp:TextBox ID="panelName" runat="server"></asp:TextBox>
        <asp:Label ID="Label3" runat="server" Text="Fields"></asp:Label>
        <asp:Table ID="tbl" runat="server" BorderStyle="Solid" GridLines="Both">
            <asp:TableRow runat="server" Font-Bold="True">
                <asp:TableCell runat="server">Column Name</asp:TableCell>
                <asp:TableCell runat="server">Include</asp:TableCell>
                <asp:TableCell runat="server">Field Type</asp:TableCell>
                <asp:TableCell runat="server" Font-Bold="true">FK displayed column</asp:TableCell>
                <asp:TableCell runat="server">Validation</asp:TableCell>
                <asp:TableCell runat="server">Caption</asp:TableCell>
            </asp:TableRow>
        </asp:Table>
        <asp:Label ID="Label2" runat="server" Text="Mappings"></asp:Label>
        <asp:Table ID="mappingsTbl" runat="server" BorderStyle="Solid" GridLines="Both">
            <asp:TableRow runat="server">
                <asp:TableCell runat="server" Font-Bold="True">Mapping Name</asp:TableCell>
                <asp:TableCell runat="server" Font-Bold="True">Include</asp:TableCell>
                <asp:TableCell runat="server" Font-Bold="True">Field Type</asp:TableCell>
                <asp:TableCell ID="TableCell1" runat="server" Font-Bold="true">Displayed column</asp:TableCell>
                <asp:TableCell runat="server" Font-Bold="True">Validation</asp:TableCell>
                <asp:TableCell runat="server" Font-Bold="True">Caption</asp:TableCell>
            </asp:TableRow>
        </asp:Table>
        <asp:Label ID="Label5" runat="server" Text="Allowed actions"></asp:Label>
        <asp:Label ID="Label4" runat="server" Text=" "></asp:Label>
        <cc1:M2NMappingControl ID="actions" runat="server" />
        <asp:Button ID="saveButton" runat="server" onclick="SaveButton_Click" 
            Text="Save" />
        <asp:LinkButton ID="backButton" runat="server">Back</asp:LinkButton>
        <asp:BulletedList ID="validationResult" runat="server">
        </asp:BulletedList>
    
    </asp:Panel>
    
</asp:Content>