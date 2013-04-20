<%@ Page Title="" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true"
    CodeBehind="Projects.aspx.cs" Inherits="_min.Sys.Projects" %>

<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
<asp:ScriptManager ID="ProjectsScriptManager" runat="server">
</asp:ScriptManager>

    <asp:UpdatePanel ID="ProjectsUpdatePanel" runat="server">
        <ContentTemplate>
    <asp:GridView ID="ProjectsGrid" runat="server" 
        onrowediting="ProjectsGrid_RowEditing" AutoGenerateColumns="false" 
                onrowupdating="ProjectsGrid_RowUpdating" DataKeyNames="id_project" 
                onrowcancelingedit="ProjectsGrid_RowCancelingEdit"
                Width="920px">
        <Columns>
                    <asp:BoundField DataField="name" HeaderText="Name" />
                    <asp:BoundField DataField="connstring_web" HeaderText="Connection string to the website" />
                    <asp:BoundField DataField="connstring_information_schema" HeaderText="Connection String to the information schema" />
                    <asp:BoundField DataField="server_type" HeaderText="Server Type" />
            <asp:CommandField ShowEditButton="true"/>
        </Columns>
    </asp:GridView>
            <asp:DetailsView ID="InsertProjectDetailsView" runat="server" CellPadding="4" 
                AutoGenerateInsertButton="true" DefaultMode="Insert"
                ForeColor="#333333" GridLines="None" Height="50px" Width="919px" 
                AutoGenerateRows="false" 
                oniteminserting="InsertProjectDetailsView_ItemInserting" 
                EnablePagingCallbacks="True">
                <AlternatingRowStyle BackColor="White" />
                <CommandRowStyle BackColor="#D1DDF1" Font-Bold="True" />
                <EditRowStyle BackColor="#2461BF" />
                <FieldHeaderStyle BackColor="#DEE8F5" Font-Bold="True" />
                <Fields>
                    <asp:BoundField DataField="name" HeaderText="Name" />
                    <asp:BoundField DataField="connstring_web" HeaderText="Connection string to the website" />
                    <asp:BoundField DataField="connstring_information_schema" HeaderText="Connection String to the information schema" />
                    <asp:BoundField DataField="server_type" HeaderText="Server Type" />
                </Fields>
                <FooterStyle BackColor="#507CD1" Font-Bold="True" ForeColor="White" />
                <HeaderStyle BackColor="#507CD1" Font-Bold="True" ForeColor="White" />
                <PagerStyle BackColor="#2461BF" ForeColor="White" HorizontalAlign="Center" />
                <RowStyle BackColor="#EFF3FB" />
            </asp:DetailsView>
        <!--
            <asp:DetailsView ID="ProjectDetailsView" DefaultMode="Edit" runat="server" Height="50px" Width="625px" Enabled="false"
                AutoGenerateRows="false">
                <Fields>
                    <asp:BoundField DataField="name" HeaderText="Name" />
                    <asp:BoundField DataField="connstring_web" HeaderText="Connection string to the website" />
                    <asp:BoundField DataField="connstring_information_schema" HeaderText="Connection String to the information schema" />
                    <asp:BoundField DataField="server_type" HeaderText="Server Type" />
                </Fields>
            </asp:DetailsView>
            -->
        </ContentTemplate>
    </asp:UpdatePanel>
</asp:Content>
