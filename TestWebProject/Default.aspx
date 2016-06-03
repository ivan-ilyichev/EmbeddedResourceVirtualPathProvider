<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="TestWebProject.WebForm1" %>
<%@ Import Namespace="System.Web.Hosting" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
    
    <%--<script src="/alert.js" type="text/javascript"></script>
    <script src="/alert2.js" type="text/javascript"></script>
    <script src="/Scripts/alert.3.js" type="text/javascript"></script>
    <script src="/Scripts/dashed.path/test.js" type="text/javascript"></script>--%>

</head>
<body>
    <%
        //var dirName = "~/Scripts/dashed.path/";
        var dirName = "~/";
        var virtualDirExists = HostingEnvironment.VirtualPathProvider.DirectoryExists(dirName);

        if (!virtualDirExists)
        {
            %>virtual directory doesn't exist<%
        }
        else
        {
            var virtualDir = HostingEnvironment.VirtualPathProvider.GetDirectory(dirName);
            var files = virtualDir.Files;
            var files2 = files.Cast<VirtualFileBase>().ToList();
            var dirs = virtualDir.Directories.Cast<VirtualDirectory>().ToList();
            %>
                <div>virtual directory exists.</div>
                <div>directories:</div>
                <ul>
            <%
            foreach (var dir in dirs)
            {
                %><li>
                        <%= dir.Name %>
                </li>
                <%
            }
            %>
                </ul>

                <div>files:</div>
                <ul>
            <%
            foreach (var file in files2)
            {
                %><li>
                        <%= file.Name %>
                </li>
                <%
            }
            %>
                </ul>
            <%
        }
    %>
</body>
</html>
