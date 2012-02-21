using System;
using System.Text;
using System.Linq;
using System.Linq.Expressions;

using Xamarin.Parse.Json;

namespace Xamarin.Parse {

	internal class QueryTranslator : ExpressionVisitor {

		JsonWriter json;
		StringBuilder sb;

		internal string Translate(Expression expression) {
			json = new JsonWriter ();
			Visit(expression);
		    return json.ToString ();
		}

		static Expression StripQuotes(Expression e) {
			while (e.NodeType == ExpressionType.Quote)
				e = ((UnaryExpression)e).Operand;
			return e;
        }

 

		protected override void VisitMethodCall(MethodCallExpression m) {
	
		    if (m.Method.DeclaringType == typeof(Queryable) && m.Method.Name == "Where") {
		
		        sb.Append("SELECT * FROM (");
		
		        this.Visit(m.Arguments[0]);
		
		        sb.Append(") AS T WHERE ");
	
		        LambdaExpression lambda = (LambdaExpression)StripQuotes(m.Arguments[1]);
		
		        this.Visit(lambda.Body);
		    }
		
		    throw new NotSupportedException(string.Format("The method '{0}' is not supported", m.Method.Name));
	
		}
	
	
	
		protected override void VisitUnary(UnaryExpression u) {
	
		    switch (u.NodeType) {
	
		        case ExpressionType.Not:
		
		            sb.Append(" NOT ");
		
		            this.Visit(u.Operand);
		
		            break;
		
		        default:
		
		            throw new NotSupportedException(string.Format("The unary operator '{0}' is not supported", u.NodeType));
	
		    }
		
		}
		
	
		
		protected override void VisitBinary(BinaryExpression b) {
		
		    sb.Append("(");
		
		    this.Visit(b.Left);
	
		    switch (b.NodeType) {
		
		        case ExpressionType.And:
		
		            sb.Append(" AND ");
		
		            break;
		
		        case ExpressionType.Or:
		
		            sb.Append(" OR");
		
		            break;
	
		        case ExpressionType.Equal:
		
		            sb.Append(" = ");
		
		            break;
		
		        case ExpressionType.NotEqual:
		
		            sb.Append(" <> ");
		
		            break;
		
		        case ExpressionType.LessThan:
	
		            sb.Append(" < ");
		
		            break;
		
		        case ExpressionType.LessThanOrEqual:
		
		            sb.Append(" <= ");
		
		            break;
		
		        case ExpressionType.GreaterThan:
		
		            sb.Append(" > ");
	
		            break;
		
		        case ExpressionType.GreaterThanOrEqual:
		
		            sb.Append(" >= ");
		
		            break;
		
		        default:
		
		            throw new NotSupportedException(string.Format("The binary operator '{0}' is not supported", b.NodeType));
		
		    }
	
		    this.Visit(b.Right);
		
		    sb.Append(")");
		
		}
		
		
		
		protected override void VisitConstant(ConstantExpression c) {
		
		    IQueryable q = c.Value as IQueryable;
	
		    if (q != null) {
		
		        // assume constant nodes w/ IQueryables are table references
		
		        sb.Append("SELECT * FROM ");
		
		        sb.Append(q.ElementType.Name);
		
		    }
		
		    else if (c.Value == null) {
		
		        sb.Append("NULL");
	
		    }
		
		    else {
		
		        switch (Type.GetTypeCode(c.Value.GetType())) {
		
		            case TypeCode.Boolean:
		
		                sb.Append(((bool)c.Value) ? 1 : 0);
		
		                break;
		
		            case TypeCode.String:
	
		                sb.Append("'");
		
		                sb.Append(c.Value);
		
		                sb.Append("'");
		
		                break;
		
		            case TypeCode.Object:
		
		                throw new NotSupportedException(string.Format("The constant for '{0}' is not supported", c.Value));
		
		            default:
	
		                sb.Append(c.Value);
		
		                break;
		
		        }
		
		    }
		
		}
		
		
	
		protected override void VisitMemberAccess(MemberExpression m) {
		
		    if (m.Expression != null && m.Expression.NodeType == ExpressionType.Parameter) {
		
		        sb.Append(m.Member.Name);
		
		        return;
		
		    }
	
		    throw new NotSupportedException(string.Format("The member '{0}' is not supported", m.Member.Name));
	
		}

	}
}

