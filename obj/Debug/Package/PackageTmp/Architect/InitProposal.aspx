<%@ Page Title="" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true" CodeBehind="InitProposal.aspx.cs" Inherits="_min.Architect.InitProposal" %>
<%@ MasterType  virtualPath="~/Site.master"%>

<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <asp:ScriptManager ID="MainScriptManager" runat="server">
    </asp:ScriptManager>
    <asp:UpdatePanel ID="MainUpdatePanel" runat="server">
        <ContentTemplate>
            <asp:Wizard ID="InitProposalWizard" runat="server" ActiveStepIndex="4" Height="159px"
                Width="920px" 
                onnextbuttonclick="InitProposalWizard_NextButtonClick" 
                OnFinishButtonClick="InitProposalWizard_FinishButtonClick">
                <WizardSteps>

                    <asp:WizardStep ID="WizardStepFatalProblems" runat="server" Title="Found problems">
                        <asp:BulletedList ID="FirstProblemList" runat="server">
                        </asp:BulletedList>
                        <asp:Button ID="RetryButton" runat="server" Text="Check again" />
                    </asp:WizardStep>

                    <asp:WizardStep ID="WizardStepTableUsage" runat="server" Title="Tables Usage">
                        <asp:GridView ID="TablesUsageGridView" runat="server" 
                            AutoGenerateColumns="False" OnRowDataBound="TablesUsageGridView_RowDataBound">
                            <Columns>
                                <asp:BoundField HeaderText="Table Name" DataField="TableName" ReadOnly="True" />
                                <asp:TemplateField HeaderText="Direct editation">
                                    <ItemTemplate>
                                        <asp:CheckBox ID="DirectEditCheck" runat="server"></asp:Checkbox>
                                    </ItemTemplate>
                                </asp:TemplateField>
                                <asp:TemplateField HeaderText="Representative column">
                                    <ItemTemplate>
                                        <asp:DropDownList ID="DisplayColumnDrop" runat="server"></asp:DropDownList>
                                    </ItemTemplate>
                                </asp:TemplateField>
                            </Columns>
                        </asp:GridView>
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
                    </asp:WizardStep>

                    <asp:WizardStep ID="WizardStepFinish" runat="server" Title="Propose!">
                        <asp:Label ID="WaitLabel" runat="server"
                            Text="Click finish and be patient...">
                        </asp:Label>
                    </asp:WizardStep>

                </WizardSteps>
            </asp:Wizard>
        </ContentTemplate>
    </asp:UpdatePanel>
</asp:Content>
