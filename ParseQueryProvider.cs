using System;
using System.Linq;
using System.Linq.Expressions;

namespace Xamarin.Parse {

	public class ParseQueryProvider : IQueryProvider {

		string path;

		public ParseQueryProvider (string path)
		{
			this.path = path;
		}

		public string GetQueryText (Expression expression)
		{
			return new QueryTranslator ().Translate (expression);
		}

        public object Execute (Expression expression)
		{
			Console.WriteLine ("Would exec: {0}", expression);
			return null;
		}

		S IQueryProvider.Execute<S>(Expression expression) {
			return (S)Execute (expression);
		}

		IQueryable<S> IQueryProvider.CreateQuery<S> (Expression expression)
		{
			return new ParseQuery<S> (this, expression);
		}

		IQueryable IQueryProvider.CreateQuery (Expression expression) {
			Console.WriteLine (expression.ToString ());
			throw new NotSupportedException ("Non-generic IQueryProvider.CreateQuery not supported.");
		}
    }
}

