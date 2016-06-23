using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Kraken {

	public static class FileSystemExtensions {
		public static bool IsDirectory(this FileSystemInfo fileSystemInfo) {
			if (fileSystemInfo == null)
				return false;

			if ((int)fileSystemInfo.Attributes != -1)
				return fileSystemInfo.Attributes.HasFlag(FileAttributes.Directory);

			return fileSystemInfo is DirectoryInfo;
		}

		public static bool IsFile(this FileSystemInfo fileSystemInfo) {
			if (fileSystemInfo == null)
				return false;

			return !IsDirectory(fileSystemInfo);
		}

		public static void AddToFile(this string line, string path) {
			File.AppendAllText(path, line + Environment.NewLine);
		}

		public static bool HasAccessToFile(string path) {
			try {
				// Attempt to get a list of security permissions from the file. 
				// This will raise an exception if the path is read only or do not have access to view the permissions. 
				File.GetAccessControl(path);
				return true;
			} catch (UnauthorizedAccessException) {
				return false;
			}
		}

		public static bool HasAccessToFolder(string path) {
			try {
				// Attempt to get a list of security permissions from the folder. 
				// This will raise an exception if the path is read only or do not have access to view the permissions. 
				Directory.GetAccessControl(path);
				var list = Directory.EnumerateFileSystemEntries(path, "*", SearchOption.AllDirectories).ToList();
				return true;
			} catch (UnauthorizedAccessException) {
				return false;
			} catch (System.Security.SecurityException) {
				return false;
			}
		}

		public static void Rename(this FileSystemInfo fi, string newName) {
			if (fi is FileInfo) {
				var ffi = fi as FileInfo;
				ffi.MoveTo(Path.Combine(ffi.Directory.FullName, newName));
			} else if (fi is DirectoryInfo) {
				var di = fi as DirectoryInfo;
				di.MoveTo(Path.Combine(di.Parent.FullName, newName));
			}
		}

		public static DirectoryInfo GetParent(this FileSystemInfo fi) {
			if (fi is FileInfo) {
				return (fi as FileInfo).Directory;
			} else if (fi is DirectoryInfo) {
				return (fi as DirectoryInfo).Parent;
			}
			return null;
		}

		public static void MoveFile(string sourcePath, string destPath) {
			if (File.Exists(destPath)) {
				File.Delete(destPath);
			}
			File.Move(sourcePath, destPath);
		}

		public static void MoveDirectory(string sourcePath, string destPath) {
			if (Directory.Exists(destPath)) {
				Directory.Delete(destPath, true);
			}
			Directory.Move(sourcePath, destPath);
		}
	}
}
