namespace Hengine.Translation
{
	public class TranslationUnit // TODO: Makeprivate
	{
		Dictionary<string, string> units;

		public TranslationUnit(Dictionary<string, string> units)
		{
			this.units = units;
		}

		public string GetTranslation(string language)
		{
			return units[language];
		}
	}

	public class TranslationManager
	{
		TranslationConfig config;
		Dictionary<string, TranslationUnit> translations = new Dictionary<string, TranslationUnit>();

		public TranslationManager(TranslationConfig config)
		{
			this.config = config;
			translations = config.units;
		}

		public string GetTranslation(string id)
		{
			return translations[id].GetTranslation(config.language);
		}
	}

	public class Translator
	{
		public static TranslationManager TranslationSetup(TranslationConfig config)
		{
			return new(config);
		}
	}
}
