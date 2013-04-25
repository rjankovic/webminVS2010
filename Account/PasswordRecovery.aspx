<%@ Page Title="PasswordRecovery" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true"
    CodeBehind="PasswordRecovery.aspx.cs" Inherits="_min.Account.PasswordRecovery" %>

<asp:Content ID="HeaderContent" runat="server" ContentPlaceHolderID="HeadContent">
</asp:Content>
<asp:Content ID="BodyContent" runat="server" ContentPlaceHolderID="MainContent">
    <asp:PasswordRecovery ID="PasswordRecovery1" runat="server" BackColor="#FFFFFF" 
        BorderColor="#AAAAAA" BorderPadding="4" BorderStyle="Solid" BorderWidth="1px" 
        Font-Size="1em">
        <SubmitButtonStyle BackColor="#FFFFFF" BorderColor="#AAA" 
            BorderStyle="Solid" BorderWidth="1px" Font-Names="Verdana" 
            ForeColor="#222222" />
        <InstructionTextStyle Font-Italic="True" ForeColor="Black" />
        <SuccessTextStyle Font-Bold="True" ForeColor="#222222" />
        <TextBoxStyle />
        <TitleTextStyle BackColor="#FFFFFF" Font-Bold="True"
            ForeColor="White" />
    </asp:PasswordRecovery>
</asp:Content>
