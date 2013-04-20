<%@ Page Title="" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Users.aspx.cs" Inherits="_min.Sys.WebForm1" %>
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
        onselectedindexchanged="SomeSelect_SelectedIndexChanged" AutoPostBack="True">
    </asp:DropDownList>
    <asp:Label ID="AdministrationLabel" runat="server" 
        Text="within"></asp:Label>
            <asp:DropDownList ID="ProjectSelect" runat="server"
            onselectedindexchanged="SomeSelect_SelectedIndexChanged" AutoPostBack="True">
            </asp:DropDownList>
            <asp:Label ID="Label1" runat="server" Text="is permitted to"></asp:Label>
    <asp:CheckBoxList ID="PermissionCheckboxList" runat="server">
        <asp:ListItem Value="10">Administer</asp:ListItem>
        <asp:ListItem Value="100">Change architecture</asp:ListItem>
        <asp:ListItem Value="1000">Manage permissions</asp:ListItem>
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
