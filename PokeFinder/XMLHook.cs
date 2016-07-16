using System;
using System.IO;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Content.Res;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace PokeFinder
{
    class XMLHook
    {

        public string[] GetPokemonArray(AssetManager Assets)
        {
            string content = "";
            using (StreamReader sr = new StreamReader(Assets.Open("Pokemon.xml")))
            {
                content = sr.ReadToEnd();
            }


            var doc = XDocument.Load(Assets.Open("Pokemon.xml"));


            var pokemon = from list in doc.Descendants("Pokemon")
                          select (string)list.Value;

            string[] arr = pokemon.ToArray<string>();

            

            return arr;
            
        }

    }
}