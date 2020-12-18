using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace PandemicPanicBot
{
    public class Nouns
    {
        // This class contains the info from the words.json file
        [JsonProperty("animals")]
        public List<string> Animals { get; private set; } = new List<string>();
        [JsonProperty("artificial")]
        public List<string> Artificial { get; private set; } = new List<string>();
        [JsonProperty("body_parts")]
        public List<string> Body_parts { get; private set; } = new List<string>();
        [JsonProperty("fruit/vegetables")]
        public List<string> Fruit_vegetables { get; private set; } = new List<string>();
        [JsonProperty("natural_world")]
        public List<string> Natural_world { get; private set; } = new List<string>();
        [JsonProperty("occupations")]
        public List<string> Occupations { get; private set; } = new List<string>();

        // Stores all nouns, to make it simple to randomly choose a noun.
        public List<string> All_nouns { get; private set; } = new List<string>();

        // The number of categories of Nouns (currently is 6)
        public int Categories { get; private set; } = 6;

        // Constructor, does nothing, but we need it to compile.
        public Nouns() { }

        // Concatonates the conceptual_nouns and physical_nouns to form all_nouns
        public void AppendAllNouns()
        {
            // Called once the .json file is read.
            if (All_nouns.Count() == 0)
            {
                foreach (var a in Animals)
                    All_nouns.Add(a);
                foreach (var mm in Artificial)
                    All_nouns.Add(mm);
                foreach (var bp in Body_parts)
                    All_nouns.Add(bp);
                foreach (var fv in Fruit_vegetables)
                    All_nouns.Add(fv);
                foreach (var nw in Natural_world)
                    All_nouns.Add(nw);
                foreach (var o in Occupations)
                    All_nouns.Add(o);
            }
        }

        // Returns a path to the image associated with term is term is a valid word,
        // otherwise returns empty string ("")
        public string FetchImagePath(string term)
        {
            string basePath = "../../../word_images/";
            if (Animals.Contains(term))
                return basePath + "animals/" + term + ".jpg";
            else if (Body_parts.Contains(term))
                return basePath + "body_parts/" + term + ".jpg";
            else if (Fruit_vegetables.Contains(term))
                return basePath + "fruit_vegetables/" + term + ".jpg";
            else if (Artificial.Contains(term))
                return basePath + "artificial/" + term + ".jpg";
            else if (Natural_world.Contains(term))
                return basePath + "nature/" + term + ".jpg";
            else if (Occupations.Contains(term))
                return basePath + "occupations/" + term + ".jpg";
            return "";
        }

        // Given a RoundTheme enum, return the string associated with RoundTheme
        // if invalid, returns empty string ("")
        public string TypeOfWord(RoundCategory r)
        {
            switch (r)
            {
                case RoundCategory.Animals:
                    return "Animals";
                case RoundCategory.Bodyparts:
                    return "Human Body Parts";
                case RoundCategory.FruitVegetables:
                    return "Fruits and Vegetables";
                case RoundCategory.Artificial:
                    return "Artificial Objects";
                case RoundCategory.NaturalWorld:
                    return "The Natural World";
                case RoundCategory.Occupations:
                    return "Jobs and Occupations";
                default:
                    return "";
            }
        }
    }
}
