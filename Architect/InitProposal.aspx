<%@ Page Title="" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true"
    CodeBehind="InitProposal.aspx.cs" Inherits="_min_t7.Architect.InitProposal" %>


<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <asp:ScriptManager ID="MainScriptManager" runat="server">
    </asp:ScriptManager>
    <asp:UpdatePanel ID="MainUpdatePanel" runat="server">
        <ContentTemplate>
            <asp:Wizard ID="InitProposalWizard" runat="server" ActiveStepIndex="2" Height="159px"
                Width="920px" BackColor="#EFF3FB" BorderColor="#B5C7DE" BorderWidth="1px" 
                Font-Names="Verdana" Font-Size="0.8em" 
                onnextbuttonclick="InitProposalWizard_NextButtonClick" 
                OnFinishButtonClick="InitProposalWizard_FinishButtonClick">
                <HeaderStyle BackColor="#284E98" BorderColor="#EFF3FB" BorderStyle="Solid" 
                    BorderWidth="2px" Font-Bold="True" Font-Size="0.9em" ForeColor="White" 
                    HorizontalAlign="Center" />
                <NavigationButtonStyle BackColor="White" BorderColor="#507CD1" 
                    BorderStyle="Solid" BorderWidth="1px" Font-Names="Verdana" Font-Size="0.8em" 
                    ForeColor="#284E98" />
                <SideBarButtonStyle BackColor="#507CD1" Font-Names="Verdana" 
                    ForeColor="White" />
                <SideBarStyle BackColor="#507CD1" Font-Size="0.9em" VerticalAlign="Top" />
                <StepStyle Font-Size="0.8em" ForeColor="#333333" />
                <WizardSteps>
                    <asp:WizardStep ID="WizardStepFatalProblems" runat="server" Title="Found problems">
                        <asp:BulletedList ID="FirstProblemList" runat="server">
                        </asp:BulletedList>
                        <asp:Button ID="RetryButton" runat="server" Text="Check again" />
                    </asp:WizardStep>
                    <asp:WizardStep ID="WizardStepHierarchies" runat="server" Title="Hierarchies">
                        <asp:BulletedList ID="ProblemListHierarchies" runat="server">
                        </asp:BulletedList>
                        <asp:Button ID="HierarchyRetryButton" runat="server" Text="Check again" />
                    </asp:WizardStep>
                    <asp:WizardStep ID="WizardStepMappings" runat="server" Title="Mappings">
                        <asp:Repeater ID="MappingsChoiceRepeater" runat="server">
                            <HeaderTemplate>
                                <asp:Label runat="server" ID="RepeaterHeading" 
                                    Text="The following tables are considered mapping (associative) tables"></asp:Label>
                            </HeaderTemplate>
                            <ItemTemplate>
                                <br />
                                <label><%# DataBinder.Eval(Container.DataItem, "mapTable")%> is probably mapping 
                                    <%# DataBinder.Eval(Container.DataItem, "myTable")%> to 
                                    <%# DataBinder.Eval(Container.DataItem, "refTable")%></label>
                                <asp:CheckBox runat="server" ID="ItemCheckBox"/>
                                Edit within <%# DataBinder.Eval(Container.DataItem, "myTable")%>"
                            </ItemTemplate>
                        </asp:Repeater>
                        
                    <asp:Label ID="WaitLabel" runat="server"  Visible="false"
                        Text="Please be patient while the proposal is generated and saved to the database">
                    </asp:Label>
                    </asp:WizardStep>
                </WizardSteps>
            </asp:Wizard>
        </ContentTemplate>
    </asp:UpdatePanel>
</asp:Content>
