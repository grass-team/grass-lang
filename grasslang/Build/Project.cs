﻿using System;
using System.IO;
using grasslang.Scripting;
using grasslang.Scripting.BaseType;
using grasslang.Scripting.DotnetType;
using grasslang.CodeModel;
using System.Collections.Generic;
using System.Linq;
namespace grasslang.Build
{
    public class Project
    {
        public Project Parent = null;
        public Engine ScriptEngine = new Engine();

        public string Name = "";
        public Service Service = null;

        public List<string> Files = new List<string>();
        public List<Project> Subprojects = new List<Project>();
        public List<Project> Dependencies = new List<Project>();
        public Project MainProject;


        // load
        public bool Loaded = false;
        public event Action OnLoaded;

        private Project findProject(string name)
        {
            if (Parent != null)
            {
                // this is not the root project.
                return Parent.findProject(name);
            }
            // find project
            var result = (from subproject in Subprojects.ToArray()
                          where subproject.Name == name
                          select subproject);
            return result.Any() ? result.First() : null;
        }
        private void runOnLoaded(Action action)
        {
            // on project loaded, do something
            if (Parent != null && Parent.Loaded == false)
            {
                Parent.OnLoaded += action;
            }
            else
            {
                action();
            }
        }

        public void InstallDependency(Project sourceProject)
        {
            if (Service != null)
            {
                // use the service to install
                Service.InstallDependency(sourceProject);
                return;
            }
            // try to use the script to install
            try
            {
                // get target
                Callable callable = ScriptEngine.RootContext["InstallDependency"] as Callable;
                callable.Invoke(new List<Scripting.Object>
                {
                    new DotnetObject(sourceProject)
                });
            }
            catch { }
            // not doing anything here...
        }
        public void SetMainProject(string name)
        {
            runOnLoaded(() =>
            {
                var project = findProject(name);
                if (project is null)
                {
                    throw new Exception("The project named \"" + name + "\" not found.");
                }
                // set main project
                MainProject = project;
            });
        }
        public void AddDependency(string name)
        {
            runOnLoaded(() =>
            {
                var project = findProject(name);
                if (project is null)
                {
                    throw new Exception("The project named \"" + name + "\" not found.");
                }
                Dependencies.Add(project);
                // let that project modify this project
                project.InstallDependency(this);
            });
        }
        public void AddSubProject(string path)
        {
            if (Parent != null)
            {
                // this is not the root project.
                Parent.AddSubProject(path);
                return;
            }
            // check path
            if (!File.Exists(path))
            {
                throw new FileNotFoundException(null, path);
            }
            // create a project object, and parse the file
            Project subProject = new Project { Parent = this };
            subProject.LoadProject(path);
            // add to subprojects
            Subprojects.Add(subProject);
        }
        public void LoadProject(string path)
        {
            // parse project
            Parser parser = new Parser
            {
                Lexer = new Lexer(File.ReadAllText(path))
            };
            parser.InitParser();
            Ast ast = parser.BuildAst();
            ScriptEngine.Eval(ast);
            // handle subproject dependencies or others
            Loaded = true;
            OnLoaded?.Invoke();
        }
        public void RunTask(string name)
        {
            runOnLoaded(() =>
            {
                // try to run task in service
                if ((Service?.RunTask(name)).GetValueOrDefault()) { return; }
                // try to run task in the script
                string functionName = "Task$" + name.ToLower();
                if (ScriptEngine.RootContext
                    .Items.ContainsKey(functionName))
                {
                    // get target
                    Callable callable = ScriptEngine.RootContext[functionName] as Callable;
                    callable.Invoke(new List<Scripting.Object> { });
                    return;
                }
                // running in main project
                if(MainProject != null)
                {
                    MainProject.RunTask(name);
                } else
                {
                    throw new Exception("The task named \"" + name + "\" not found.");
                }
            });
        }
        public Project()
        {
            ScriptEngine.RootContext["Project"]
                = new DotnetObject(this);
        }
    }
}
