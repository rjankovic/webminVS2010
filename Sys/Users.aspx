<%@ Page Title="" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Users.aspx.cs" Inherits="_min_t7.Sys.WebForm1" %>
<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
    <h1>Manage users</h1>
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <asp:ScriptManager ID="UsersScriptManager" runat="server" 
        EnablePageMethods="True">
    </asp:ScriptManager>
            <asp:UpdatePanel ID="UsersUpdate" runat="server">
        <ContentTemplate>
    <asp:DropDownList ID="UserSelect" runat="server" 
        onselectedindexchanged="UserSelect_SelectedIndexChanged" AutoPostBack="True">
    </asp:DropDownList>
    <asp:Label ID="AdministrationLabel" runat="server" 
        Text="Administration Permissions For"></asp:Label>
    <asp:CheckBoxList ID="AdministerCheckboxList" runat="server">
    </asp:CheckBoxList>
    <asp:Label ID="ArchitectLabel" runat="server" Text="Architect Permissions For"></asp:Label>
    <asp:CheckBoxList ID="ArchitectCheckboxList" runat="server">
    </asp:CheckBoxList>
    <asp:Button ID="PermissionsSubmit" runat="server" Text="Save" 
        onclick="PermissionsSubmit_Click" />
        </ContentTemplate>
        <Triggers>
            <asp:AsyncPostBackTrigger ControlID="UserSelect" EventName="SelectedIndexChanged" />
        </Triggers>
        </asp:UpdatePanel>
&nbsp;
    
</asp:Content>
