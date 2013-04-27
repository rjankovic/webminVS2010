<%@ Page Title="" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true" CodeBehind="EditPanels.aspx.cs" Inherits="_min.Architect.EditPanels" %>
<%@ MasterType  virtualPath="~/Site.master"%>

<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
<asp:ScriptManager ID="ScriptManager1" runat="server">
    </asp:ScriptManager>
    <asp:Panel ID="MainPanel" runat="server">
        <asp:UpdatePanel ID="UpdatePanel1" runat="server">
            <ContentTemplate>
                <asp:GridView ID="TablesGrid" runat="server" 
                    onselectedindexchanged="TablesGrid_SelectedIndexChanged">
                    <Columns>
                        <asp:ButtonField CommandName="Select" HeaderText="Add" ShowHeader="True" 
                            Text="Add" />
                        <asp:ButtonField CommandName="Select" HeaderText="Remove" ShowHeader="True" 
                            Text="Remove" />
                    </Columns>
                </asp:GridView>
                <asp:Label ID="MappingsLabel" runat="server" Text="Mappings"></asp:Label>
                <asp:CheckBoxList ID="MappingsCheck" runat="server">
                </asp:CheckBoxList>
                <asp:Button ID="SaveButton" runat="server" Enabled="False" 
                    onclick="SaveButton_Click" Text="Confirm" />
                <asp:LinkButton ID="BackButton" runat="server">Back</asp:LinkButton>
            </ContentTemplate>
        </asp:UpdatePanel>
    
    </asp:Panel>
    
</asp:Content>