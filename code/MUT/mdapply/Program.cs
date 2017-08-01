﻿using System;

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MUT;
using System.IO;

namespace mdapply
{
    using MUT;

    class Program
    {
        static void Main(string[] args)
        {
            // Load options
            var opts = new Options(args);

            // For debugging, remove from production
            opts.PrintOptions();

            var y = new YMLMeister();
            var currentFile = "";
            var currentContent = "";
            var currentBody = "";
            var currentTagList = new Tags();
            Command command = null;

            string commandFile = File.ReadAllText(opts.ArgFile);

            var commandRecords = commandFile.Split('\n');
            foreach(var commandRecord in commandRecords)
            {
                var trimmedCommand = commandRecord.Trim();
                if (!String.IsNullOrEmpty(trimmedCommand) && trimmedCommand != CommandBuilder.Header)
                {
                    command = new Command(trimmedCommand);
                    if (currentFile != "" && currentFile != command.filename)
                    {
                        WriteCurrentFile(opts, currentFile, currentBody, currentTagList);
                    }
                    if (currentFile != command.filename)
                    {
                        currentFile = command.filename;
                        currentContent = File.ReadAllText(currentFile);
                        currentTagList = new Tags(YMLMeister.ParseYML2(currentContent));
                        currentBody = currentContent.Substring(currentContent.IndexOf("---", 4) + 3);
                    }
                    switch (command.action)
                    {
                        case Command.Action.create:
                            AddTagIfNotPresent(currentTagList, command);
                            break;
                        case Command.Action.delete:
                            DeleteTag(currentTagList, command);
                            break;
                        case Command.Action.overwrite:
                            OverwriteIfTagExists(currentTagList, command);
                            break;
                        case Command.Action.require:
                            OverwriteOrAddTag(currentTagList, command);
                            break;
                        case Command.Action.excise:
                            ExciseValues(currentTagList, command);
                            break;
                        case Command.Action.merge_if:
                            MergeValuesIfTagExists(currentTagList, command);
                            break;
                        case Command.Action.merge_add:
                            MergeValuesOrAddTag(currentTagList, command);
                            break;
                        default:
                            Console.WriteLine("Ignoring the {0} tag in {1}",
                                command.tagData.TagName, command.filename);
                            break;
                    }

                }
            }
            if (command != null && currentFile != "")
            {
                // We're done, write the file out, if there's anything to write.
                WriteCurrentFile(opts, currentFile, currentBody, currentTagList);
            }


            // open commands file or report failure and exit
            // create objects for applies-to file, metadata collection
            // for each line in commands file
            //   load fields from line or report failure and continue
            //   if first line or filename field is not the current applies-to file
            //     if filename field is not the current applies-to file
            //       if there's an updated metadata collection
            //         write the metadata collection to the applies-to file or report failure
            //       close current applies-to file
            //     open new applies-to file or report failure and continue
            //     parse metadata collection from applies-to file or report failure and continue
            //   apply action to metadata collection or report failure and continue
            // if there's an open applies-to file
            //   if there's an updated metadata collection
            //     write the metadata collection to the applies-to file or report failure
            //   close current applies-to file
            // report complete and exit

            // debug trap so you can see it at work; remove from production
            //Console.Write("Press any key to continue... ... ...");
            //Console.ReadLine();

        }

        private static void AddTagIfNotPresent(Tags currentTagList, Command command)
        {
            if (null == currentTagList.TryGetTag(command.tagData.TagName))
            {
                currentTagList.TagList.Add(command.tagData);
                currentTagList.Changed = true;
                Console.WriteLine("Adding a {0} tag to {1}",
                    command.tagData.TagName, command.filename);
            }
            else
            {
                Console.WriteLine("Attempted to add a {0} tag that already exists in {1}",
                    command.tagData.TagName, command.filename);
            }
        }

        private static void DeleteTag(Tags currentTagList, Command command)
        {
            var tagToDelete = currentTagList.TryGetTag(command.tagData.TagName);
            if (null != tagToDelete)
            {
                currentTagList.TagList.Remove(tagToDelete);
                currentTagList.Changed = true;
                Console.WriteLine("Removing a {0} tag from {1}",
                    command.tagData.TagName, command.filename);
            }
            else
            {
                Console.WriteLine("Attempted to delete a {0} tag that doesn't exist in {1}",
                    command.tagData.TagName, command.filename);
            }
        }

        private static void OverwriteIfTagExists(Tags currentTagList, Command command)
        {
            var tagToOverwrite = currentTagList.TryGetTag(command.tagData.TagName);
            if (null != tagToOverwrite)
            {
                tagToOverwrite.TagValues = command.tagData.TagValues;
                tagToOverwrite.TagFormat = command.tagData.TagFormat;
                currentTagList.Changed = true;
                Console.WriteLine("Overwriting the {0} tag in {1}",
                    command.tagData.TagName, command.filename);
            }
            else
            {
                Console.WriteLine("Attempted to overwrite a {0} tag that doesn't exist in {1}",
                    command.tagData.TagName, command.filename);
            }
        }

        private static void OverwriteOrAddTag(Tags currentTagList, Command command)
        {
            var tagToRequire = currentTagList.TryGetTag(command.tagData.TagName);
            if (null != tagToRequire)
            {
                tagToRequire.TagValues = command.tagData.TagValues;
                tagToRequire.TagFormat = command.tagData.TagFormat;
                currentTagList.Changed = true; // Might actually be the same.
                Console.WriteLine("Replacing the {0} tag in {1}",
                    command.tagData.TagName, command.filename);
            }
            else
            {
                currentTagList.TagList.Add(command.tagData);
                currentTagList.Changed = true;
                Console.WriteLine("Inserting a {0} tag in {1}",
                    command.tagData.TagName, command.filename);
            }
        }

        private static void MergeValuesOrAddTag(Tags currentTagList, Command command)
        {
            var tagToMergeTo = currentTagList.TryGetTag(command.tagData.TagName);
            if (null != tagToMergeTo)
            {
                if (tagToMergeTo.TagFormat != command.tagData.TagFormat)
                {
                    tagToMergeTo.TagFormat = command.tagData.TagFormat;
                    currentTagList.Changed = true;
                }
                foreach (var val in command.tagData.TagValues)
                {
                    if (!tagToMergeTo.TagValues.Contains(val))
                    {
                        tagToMergeTo.TagValues.Add(val);
                        currentTagList.Changed = true;
                    }
                }
                Console.WriteLine("Merged to {0} tag in {1}",
                    command.tagData.TagName, command.filename);
            }
            else
            {
                currentTagList.TagList.Add(command.tagData);
                currentTagList.Changed = true;
                Console.WriteLine("Inserting a merge {0} tag in {1}",
                    command.tagData.TagName, command.filename);
            }
        }

        private static void MergeValuesIfTagExists(Tags currentTagList, Command command)
        {
            var tagToMergeInto = currentTagList.TryGetTag(command.tagData.TagName);
            if (null != tagToMergeInto)
            {
                if (tagToMergeInto.TagFormat != command.tagData.TagFormat)
                {
                    tagToMergeInto.TagFormat = command.tagData.TagFormat;
                    currentTagList.Changed = true;
                }
                foreach (var val in command.tagData.TagValues)
                {
                    if (!tagToMergeInto.TagValues.Contains(val))
                    {
                        tagToMergeInto.TagValues.Add(val);
                        currentTagList.Changed = true;
                    }
                }
                Console.WriteLine("Merged into {0} tag in {1}",
                    command.tagData.TagName, command.filename);
            }
            else
            {
                Console.WriteLine("Attempted to merge missing {0} tag in {1}",
                    command.tagData.TagName, command.filename);
            }
        }

        private static void ExciseValues(Tags currentTagList, Command command)
        {
            var tagToExciseFrom = currentTagList.TryGetTag(command.tagData.TagName);
            if (null != tagToExciseFrom)
            {
                var newTagValues = new List<string>();
                if (tagToExciseFrom.TagFormat != command.tagData.TagFormat)
                {
                    tagToExciseFrom.TagFormat = command.tagData.TagFormat;
                    currentTagList.Changed = true;
                }
                foreach (var val in tagToExciseFrom.TagValues)
                {
                    if (!command.tagData.TagValues.Contains(val))
                    {
                        newTagValues.Add(val);
                    }
                    else
                    {
                        currentTagList.Changed = true;
                    }
                }

                if (newTagValues.Count > 0)
                {
                    tagToExciseFrom.TagValues = newTagValues;
                    Console.WriteLine("Excising from {0} tag in {1}",
                        command.tagData.TagName, command.filename);
                }
                else
                {
                    currentTagList.TagList.Remove(tagToExciseFrom);
                    Console.WriteLine("Excised entire {0} tag from {1}",
                        command.tagData.TagName, command.filename);
                }
            }
            else
            {
                Console.WriteLine("Attempted to excise from missing {0} tag in {1}",
                    command.tagData.TagName, command.filename);
            }
        }

        private static void WriteCurrentFile(Options opts, string currentFile, string currentBody, Tags currentTagList)
        {
            // We're done with the file;
            // write the file out, if there are any changes to write.
            var newContent = new StringBuilder();
            newContent.AppendLine("---");
            if (currentTagList.Count > 0)
            {
                foreach (var currentTag in currentTagList.TagList)
                {
                    if (opts.OptBracket && currentTag.TagFormat == Tag.TagFormatType.dash)
                    {
                        currentTag.TagFormat = Tag.TagFormatType.bracket;
                        currentTagList.Changed = true;
                    }
                    else if (opts.OptDash && currentTag.TagFormat == Tag.TagFormatType.bracket)
                    {
                        currentTag.TagFormat = Tag.TagFormatType.dash;
                        currentTagList.Changed = true;
                    }
                    var tagOutput = currentTag.ToString();
                    newContent.Append(tagOutput);
                }

            }
            newContent.Append("---");
            newContent.Append(currentBody);

            if (currentTagList.Changed)
            {
                File.WriteAllText(currentFile + opts.OptSuffix, newContent.ToString());
            }
        }
    }
}