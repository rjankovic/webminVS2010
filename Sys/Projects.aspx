<%@ Page Title="" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true"
    CodeBehind="Projects.aspx.cs" Inherits="_min.Sys.Projects" %>

<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
<asp:ScriptManager ID="ProjectsScriptManager" runat="server">
</asp:ScriptManager>

    <asp:UpdatePanel ID="ProjectsUpdatePanel" runat="server">
        <ContentTemplate>
    <asp:GridView ID="ProjectsGrid" runat="server" 
        onrowediting="ProjectsGrid_RowEditing" AutoGenerateColumns="false" DataKeyNames="id_project"
                Width="920px">
        <Columns>
            <asp:CommandField ShowEditButton="true" EditText="Manage"/>
                <asp:BoundField DataField="name" HeaderText="Name" />
                <asp:BoundField DataField="connstring_web" HeaderText="Connection to the website" />
                <asp:BoundField DataField="connstring_information_schema" HeaderText="Connection to the information schema" />
                <asp:BoundField DataField="server_type" HeaderText="Server Type" />
        </Columns>
    </asp:GridView>
        
            <asp:Button ID="InserButton" runat="server" onclick="InserButton_Click" 
                Text="Insert" />
        
        </ContentTemplate>
    </asp:UpdatePanel>
</asp:Content>
