---
uid: dev_config
---

# Configuring a Development System

Sensus is built on top of the [Xamarin](http://www.xamarin.com) framework. The reason for this is two-fold:  first, we aim for a 
mobile sensing system that runs on Android and iOS; second, we do not have a large software development team to reimplement Sensus 
from scratch on each mobile OS. Xamarin allows us to write the vast majority of our code one time and run it natively on each mobile 
OS. This includes the user interface. A small amount of functionality must be reimplemented for each OS (e.g., interacting with the 
hardware sensors), but the overall economy of our approach is substantial. Xamarin is free, and C# (Xamarin's high-level language) 
is a full-featured language that is similar to Java.

# Setup
1. After [installing](https://store.xamarin.com) Xamarin (targeting the Xamarin Studio Community IDE or Visual Studio), you should 
   open Xamarin/Visual Studio and grab any remaining updates for the environment.
1. Fork the official Sensus repository to your personal GitHub account and then clone the Sensus repository from your personal 
   GitHub account to your local machine. This setup will allow you to commit changes back to your GitHub repository and create 
   pull requests from your repository to the official Sensus repository.
1. Open the Sensus solution within Xamarin/Visual Studio. The IDE should automatically restore the packages required for the 
   solution. There are sometimes issues with packages that we have had to address by building our own private versions of the 
   officially distributed packages (e.g., Nugets). See the `ReferenceFixes` directory for more information and directions for 
   fixing these issues.
1. You should now be able to build the solution. After building, close Xamarin/Visual Studio and check that no modifications 
   have been made to your local repository. If any changes have been made, they are probably due to reordering of certain files 
   (e.g., project references within the .csproj files). Since the order of these files does not matter, you can safely `git checkout .` from 
   the root of your Sensus repository to undo these changes.
1. Lastly, create a symbolic link from `Scripts/git-pre-commit.sh` to `.git/hooks/pre-commit` in your local Sensus repository 
   and make the script executable. For example:
   
   ```
   ln -s [FULL PATH TO SENSUS REPO]/Scripts/git-pre-commit.sh [FULL PATH TO SENSUS REPO]/.git/hooks/pre-commit
   chmod +x [FULL PATH TO SENSUS REPO]/.git/hooks/pre-commit
   ```

You now have a clean, compiling version of Sensus.