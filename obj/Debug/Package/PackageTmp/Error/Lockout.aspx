<%@ Page Title="" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true" CodeBehind="Lockout.aspx.cs" EnableEventValidation="false" Inherits="_min.Error.Lockout"%>
<%@ MasterType  virtualPath="~/Site.master"%>

<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
        <script src="/Scripts/jquery-1.4.1.min.js" type="text/javascript"></script>
        <script src="/Scripts/M2NShift.js?<%=DateTime.Now.Ticks.ToString()%>" type="text/javascript"></script>
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
<asp:ScriptManager ID="ScriptManager1" runat="server">
    </asp:ScriptManager>
    <asp:Panel ID="MainPanel" runat="server">
        <asp:Label ID="lockoutNote" runat="server" Text="Label" CssClass="lockoutNote"></asp:Label>
    
    </asp:Panel>

</asp:Content>