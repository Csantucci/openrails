﻿// COPYRIGHT 2009, 2010, 2011, 2012, 2013, 2014, 2015 by the Open Rails project.
// 
// This file is part of Open Rails.
// 
// Open Rails is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Open Rails is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Open Rails.  If not, see <http://www.gnu.org/licenses/>.

using System.Collections.Generic;


namespace ORTS
{
    public class Notifications
    {
        public List<Notification> NotificationList = new List<Notification>();
        public List<Check> CheckList = new List<Check>();
    }

    class JsonInput
    {
        public List<Notification> NotificationList { get; set; }
        public List<Check> CheckList { get; set; }
    }

    public class Notification
    {
        public string Date { get; set; }
        public string Title { get; set; }
        public string UpdateMode { get; set; }
        public List<Item> ItemList { get; set; }
    }
    class Record : Item
    {
        public string Value { get; set; }
    }
    class Text : Item
    {
    }
    class Heading : Item
    {
        new public string Color { get; set; } = "blue";
    }
    class Link : Item
    {
        public string Value { get; set; }
        public string Url { get; set; }
        public string StableUrl { get; set; }
        public string TestingUrl { get; set; }
        public string UnstableUrl { get; set; }
    }
    class Update : Item
    {
        public string Value { get; set; }
        public string UpdateMode { get; set; }
    }
    public class Item
    {
        public List<string> IncludeIf { get; set; }
        public List<string> IncludeIfNot { get; set; }
        public string Label { get; set; }
        public string Color { get; set; } = "black";
        public int Indent { get; set; } = 140;
    }
    //public class Met
    //{
    //    public List<Item> ItemList { get; set; }
    //    public List<CheckId> CheckIdList { get; set; }
    //}
    //public class CheckId
    //{
    //    public string Id { get; set; }
    //}

    public class Check
    {
        public string Id { get; set; }
        public List<AnyOf> AnyOfList { get; set; }
        //public List<Item> UnmetItemList { get; set; }
    }

    //public class CheckAllOf
    //{
    //    public List<Criteria> AllOfList { get; set; }
    //}
    public class AnyOf
    {
        public List<Criteria> AllOfList { get; set; }
    }

    //public class Excludes : CheckAllOf
    //{
    //}

    //public class Includes : CheckAllOf
    //{
    //}

    class Contains : Criteria { }
    class NotContains : Criteria { }

    // Not implemented yet
    // String comparison, not numerical
    class NoLessThan : Criteria { }
    class NoMoreThan : Criteria { }
    
    public class Criteria
    {
        // System Information "examples"
        public string Property { get; set; }    // installed_version, direct3d, runtime, system, memory, cpu, gpu
        public string Value { get; set; }       // {{new_version}}, {{10_0}}
    }

    public class ParameterValue
    {
        public string Parameter { get; set; }    // installed_version, direct3d, runtime, system, memory, cpu, gpu
        public string Value { get; set; }       // {{new_version}}, {{10_0}}
    }

    public class OverrideParameterList
    {
        public List<ParameterValue> ParameterValueList = new List<ParameterValue>();
    }
}
