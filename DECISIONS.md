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

### איפה דחיתי/תיקנתי הצעה של AI
- מה AI הציע, למה זה היה שגוי, ומה עשיתי במקום:

### אבטחה
- אם מצאתם בעיית אבטחה: מה מצאתם, איפה (קובץ + שורה), למה זו בעיה, ואיך תיקנתם:

## 6. הוראות הרצה
- (אם שיניתם משהו מהוראות ה‑README המקורי)
- באיזה SQL השתמשתי בפועל: ברירת המחדל של הפרויקט היא **PostgreSQL** (`docker compose up` מרים `postgres:16-alpine`, וב-`Program.cs` נעשה `UseNpgsql` כשלא מוגדר אחרת). מכיוון שלא היה Docker זמין בסביבת הפיתוח שלי, הרצתי ובדקתי הכל מול **SQLite** (`Database__Provider=Sqlite dotnet run` / `dotnet test`), כפי שה-README מגדיר כחלופה. אימתתי שה-API עולה, זורע נתונים, ומחזיר תשובות תקינות (כולל `404`/`409` ב-`approve`) גם מול SQLite.

</div>
