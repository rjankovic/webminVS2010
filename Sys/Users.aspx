<%@ Page Title="" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true" CodeBehind="Users.aspx.cs" Inherits="_min.Sys.WebForm1" %>
<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
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
            <ul class="noBullets">
            <li><asp:CheckBox ID="AdministerCb" runat="server" />
            <asp:Label ID="AdministerLabel" runat="server" Text="Administer"></asp:Label>
            </li><li><asp:CheckBox ID="ArchitectCb" runat="server" />
            <asp:Label ID="ArchitectLable" runat="server" Text="Change architecture"></asp:Label>
            </li><li><asp:CheckBox ID="PermitCb" runat="server" />
            <asp:Label ID="PermitLabel" runat="server" Text="Manage User Permissions"></asp:Label>
            </ul>
    <asp:Button ID="PermissionsSubmit" runat="server" Text="Save" 
        onclick="PermissionsSubmit_Click" />
        </ContentTemplate>
        <Triggers>
            <asp:AsyncPostBackTrigger ControlID="UserSelect" EventName="SelectedIndexChanged" />
        </Triggers>
        </asp:UpdatePanel>
&nbsp;
    
</asp:Content>
