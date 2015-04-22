using Microsoft.Web.XmlTransform;
using Sitecore.Diagnostics;
using Sitecore.Install.Framework;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Sitecore.Strategy.Packages.PostSteps
{
    public class XdtTransformer : IPostStep
    {
        protected virtual IDictionary<string, string> GetTargetsAndSources(ITaskOutput output, IDictionary<string, string> attributes)
        {
            var targetPath = GetFilePathFromAttribute("target", attributes);
            if (string.IsNullOrEmpty(targetPath))
            {
                Log.Error("No attribute 'target' is specified or the value is missing", this);
            }
            //
            var sourcePath = GetFilePathFromAttribute("source", attributes);
            if (string.IsNullOrEmpty(sourcePath))
            {
                Log.Error("No attribute 'source' is specified or the value is missing", this);
            }
            //
            if (string.IsNullOrEmpty(targetPath) || string.IsNullOrEmpty(sourcePath))
            {
                return null;
            }
            var data = new Dictionary<string, string>();
            data[sourcePath] = targetPath;
            return data;
        }

        protected virtual string GetFilePathFromAttribute(string name, IDictionary<string, string> attributes)
        {
            var attr = attributes[name];
            if (string.IsNullOrEmpty(attr))
            {
                return null;
            }
            var rootPath = AppDomain.CurrentDomain.BaseDirectory;
            var filePath = Path.Combine(rootPath, attr);
            if (!File.Exists(filePath))
            {
                return null;
            }
            return filePath;
        }

        protected virtual void BackupTargetFiles(IEnumerable<string> paths)
        {
            var backupExt = DateTime.Now.ToString("yyyyMMddhhmmss");
            var fileNames = new Dictionary<string, string>();
            foreach (var path in paths)
            {
                var folder = Path.GetDirectoryName(path);
                var fileName = Path.GetFileName(path);
                var backupFileName = string.Format("{0}.bak.{1}", fileName, backupExt);
                var backupFilePath = Path.Combine(folder, backupFileName);
                Log.Info(string.Format("Starting target file backup: {0} to {1}", path, backupFilePath), this);
                var doc = new XmlDocument();
                doc.Load(path);
                doc.Save(backupFilePath);
                Log.Info(string.Format("Target file backup complete: {0} to {1}", path, backupFilePath), this);
            }
        }

        protected virtual void TransformFiles(IDictionary<string, string> data)
        {
            if (data == null || data.Count == 0)
            {
                Log.Warn("No files were specified to be transformed", this);
                return;
            }
            foreach (var sourceFilePath in data.Keys)
            {
                using (var stream = new FileStream(sourceFilePath, FileMode.Open))
                {
                    var t = new XmlTransformation(stream, null);
                    var doc = new XmlDocument();
                    var targetFilePath = data[sourceFilePath];
                    doc.Load(targetFilePath);
                    Log.Info(string.Format("Starting transformation: {0} to {1}", sourceFilePath, targetFilePath), this);
                    t.Apply(doc);
                    Log.Info(string.Format("Transformation complete: {0} to {1}", sourceFilePath, targetFilePath), this);
                    Log.Info(string.Format("Starting save: {0}", targetFilePath), this);
                    doc.Save(targetFilePath);
                    Log.Info(string.Format("Save complete: {0}", targetFilePath), this);
                }
            }
        }
        public virtual void Run(ITaskOutput output, NameValueCollection metaData)
        {
            var attributes = metaData["Attributes"].Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries)
               .Select(part => part.Split('='))
               .ToDictionary(split => split[0], split => split[1]);
            var data = GetTargetsAndSources(output, attributes);
            if (data == null || data.Count == 0)
            {
                Log.Warn("No target and source files were located so post step is aborting", this);
                return;
            }
            //
            var fileNamesForDisplay = new StringBuilder();
            var targetFilePaths = data.Values.Distinct();
            foreach (var targetPath in targetFilePaths)
            {
                fileNamesForDisplay.AppendFormat("* {0}<br/>", targetPath);
            }
            var response = output.Confirm(string.Format("Are you sure you want to modify the following files? If you click OK the following files will be backed up and modified:<br/>{0}", fileNamesForDisplay));
            if (response != "yes")
            {
                Log.Warn("The user declined to allow the files to be modified", this);
                output.Alert(string.Format("The following files will not be modified. You may need to change them manually:<br/>{0}", fileNamesForDisplay));
                return;
            }
            //
            BackupTargetFiles(targetFilePaths);
            TransformFiles(data);
        }
    }
}
