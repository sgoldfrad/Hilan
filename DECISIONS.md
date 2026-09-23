<div dir="rtl">

# DECISIONS

> מלאו את הקובץ הזה. הוא חלק מההערכה — קצר וברור עדיף על ארוך.

## 1. החלטות ארכיטקטוניות
- הפרדת שכבות ב-Controller: `LeaveRequestsController` עשה גישה ל-DB, לוגיקה עסקית ו-validation באותו מקום. הוצאתי את כל הלוגיקה העסקית (חישוב ימים, בדיקת מכסה, בדיקת סטטוס + הטרנזקציה/lock ב-approve) ל-`ILeaveRequestService`/`LeaveRequestService` (backend/src/LeaveManagement.Api/Services/LeaveRequestService.cs), שעובד ישירות מול `LeaveDbContext`. ה-Controller נשאר דק — ממפה DTO לקריאה לסרוויס וממיר את תוצאת ה-`LeaveRequestOperationResult` (enum status) לקוד HTTP מתאים. לא הוספתי שכבת repository נוספת מעל ה-DbContext: EF Core כבר מספק unit-of-work/repository בעצמו, ועוד שכבה מעליו הייתה over-engineering מיותר לפרויקט בגודל הזה.

## 2. הבאג ביתרת החופשה
- מה היה הבאג, איפה, ואיך תיקנתי: ב-`LeaveRequestsController.Create` (backend/src/LeaveManagement.Api/Controllers/LeaveRequestsController.cs) הבדיקה של המכסה השוותה רק את מספר הימים המבוקשים (`days`) מול `employee.AnnualQuota`, בלי להביא בחשבון ימי חופשה שכבר אושרו (`used`). כך עובד יכול היה להגיש בקשה שחורגת מהמכסה כל עוד הבקשה הבודדת עצמה לא חרגה. התיקון: שינוי התנאי ל-`used + days > employee.AnnualQuota`.
- הטסט שמוכיח את התיקון: `Create_ExceedingRemainingQuota_ReturnsBadRequest` ב-`backend/tests/LeaveManagement.Tests/LeaveRequestsTests.cs` — יוצר עובד עם מכסה של 10 ימים, מוסיף בקשה מאושרת בת 8 ימים, ואז מנסה להגיש בקשה נוספת של 3 ימים. מוודא שמתקבל `BadRequestObjectResult` ושלא נוצרה בקשה חדשה במסד הנתונים.

## 3. אישור בקשה (approve) ו‑concurrency
- איך טיפלתי במצבים לא חוקיים (כבר אושר / לא קיים): `POST /api/leave-requests/{id}/approve` מחזיר `404 NotFound` אם הבקשה לא קיימת, ו-`409 Conflict` אם הסטטוס שלה כבר שונה מ-`Pending` (כלומר כבר `Approved` או `Rejected`).
- מה לגבי אישור של שתי בקשות במקביל: הכל רץ בתוך `BeginTransactionAsync`. ב-PostgreSQL מבצעים לפני הקריאה `SELECT ... FOR UPDATE` על השורה, כך שהעסקה השנייה שמנסה לאשר את אותה בקשה נחסמת עד שהראשונה עושה commit/rollback, ורק אז קוראת סטטוס מעודכן — כך שרק בקשה אחת מצליחה והשנייה מקבלת `409`. ב-SQLite אין `FOR UPDATE`; SQLite ממילא נועל את כל הקובץ לכתיבה בזמן טרנזקציה (writer יחיד בכל רגע נתון), כך שבפועל שתי קריאות `approve` בו-זמנית עדיין מסתדרות בתור ולא רצות ממש במקביל — אבל זו נעילת קובץ גסה ולא נעילת שורה אמיתית. במסד נתונים אמיתי עם נעילות (כמו PostgreSQL) הפתרון הוא בדיוק ה-`SELECT ... FOR UPDATE` שמומש כאן.

## 4. על מה ויתרתי בגלל הזמן
- ... ומה הייתי עושה עם עוד יום:

## 5. שימוש ב‑AI
### איפה AI עזר (כולל prompts)
1. prompt: "Bug fix: There is a bug in the vacation balance check—an employee can submit a request that exceeds the annual quota, and the system accepts it. The issue is located in `LeaveRequestsController` within the // POST /api/leave-requests [HttpPost] request; the logic needs to be updated to validate against the days already used." → קיבלתי את התיקון המדויק ל-`Create` (השוואה מול `used + days` במקום רק `days`), ואימצתי אותו כמו שהוא.
2. prompt: "Please also add a test case that demonstrates the fix." → קיבלתי טסט חדש (`Create_ExceedingRemainingQuota_ReturnsBadRequest`) שממחיש את התיקון, הרצתי אותו ווידאתי שהוא עובר.
3. prompt: "leav-requests form with validation: start date is not later than end date, leave type is mandatory, no negative number of days. Clear error messages." → קיבלתי טופס "בקשה חדשה" מלא ב-`leave-requests.component.ts`/`.html`/`.css` (frontend/src/app/leave-requests), בנוי עם Angular Reactive Forms: שדות חובה לעובד/סוג חופשה/תאריכי התחלה-סיום, ו-validator ברמת ה-`FormGroup` שחוסם שליחה כשתאריך ההתחלה מאוחר מתאריך הסיום (מה שמונע גם מספר ימים שלילי), עם הודעות שגיאה ברורות לצד כל שדה.

### איפה דחיתי/תיקנתי הצעה של AI
- ה-AI הציע לפצל את טופס "בקשה חדשה" (בתוך `leave-requests.component.ts`/`.html`) לקומפוננטה נפרדת (`NewLeaveRequestComponent` עם `@Input`/`@Output`) כדי להפריד אחריות (single responsibility) בין הצגת הרשימה ליצירת בקשה. דחיתי את ההצעה ואמרתי "נשאיר ככה" - זו החלטה סבירה: זה POC קטן, ופיצול מוקדם מדי לפעמים מוסיף boilerplate מיותר בלי תועלת אמיתית בשלב הזה.

### אבטחה
- prompt: "עכשיו תעבור על הפרויקט בעיניים של סייבר ותראה האם אתה מוצא בעיות אבטחה." → ה-AI זיהה **SQL Injection** ב-`LeaveRequestService.Search` (backend/src/LeaveManagement.Api/Services/LeaveRequestService.cs): ה-endpoint `GET /api/leave-requests/search?name=...` בנה שאילתת SQL על ידי concatenation ישיר של הפרמטר `name` שמגיע מהמשתמש לתוך מחרוזת, והריץ אותה עם `FromSqlRaw`. כל אחד יכול היה להעביר ב-`name` מחרוזת כמו `x' OR '1'='1` או `'; DROP TABLE "Employees"; --` ולשנות את הלוגיקה של השאילתה, לחלץ נתונים שלא אמורים להיות נגישים, או למחוק/לשנות נתונים - קלאסי OWASP Top 10 (Injection). התיקון (שגם אותו ה-AI ביצע): הסרת ה-SQL הגולמי לגמרי והחלפתו בשאילתת LINQ (`_db.LeaveRequests.Include(r => r.Employee).Where(r => r.Employee != null && EF.Functions.Like(r.Employee.Name, $"%{name}%"))`), כך ש-EF Core בונה שאילתה פרמטרית מתחתיו ולא מחרוזת SQL חופשית. בנוסף נוספה ב-`LeaveRequestsController.Search` בדיקת `string.IsNullOrWhiteSpace(name)` שמחזירה `400 BadRequest` במקום לתת לשאילתה לרוץ עם קלט ריק/null.

## 6. הוראות הרצה
- (אם שיניתם משהו מהוראות ה‑README המקורי)
- באיזה SQL השתמשתי בפועל: ברירת המחדל של הפרויקט היא **PostgreSQL** (`docker compose up` מרים `postgres:16-alpine`, וב-`Program.cs` נעשה `UseNpgsql` כשלא מוגדר אחרת). מכיוון שלא היה Docker זמין בסביבת הפיתוח שלי, הרצתי ובדקתי הכל מול **SQLite** (`Database__Provider=Sqlite dotnet run` / `dotnet test`), כפי שה-README מגדיר כחלופה. אימתתי שה-API עולה, זורע נתונים, ומחזיר תשובות תקינות (כולל `404`/`409` ב-`approve`) גם מול SQLite.

</div>
