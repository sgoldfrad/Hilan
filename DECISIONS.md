<div dir="rtl">

# DECISIONS

> מלאו את הקובץ הזה. הוא חלק מההערכה — קצר וברור עדיף על ארוך.

## 1. החלטות ארכיטקטוניות
- ...

## 2. הבאג ביתרת החופשה
- מה היה הבאג, איפה, ואיך תיקנתי: ב-`LeaveRequestsController.Create` (backend/src/LeaveManagement.Api/Controllers/LeaveRequestsController.cs) הבדיקה של המכסה השוותה רק את מספר הימים המבוקשים (`days`) מול `employee.AnnualQuota`, בלי להביא בחשבון ימי חופשה שכבר אושרו (`used`). כך עובד יכול היה להגיש בקשה שחורגת מהמכסה כל עוד הבקשה הבודדת עצמה לא חרגה. התיקון: שינוי התנאי ל-`used + days > employee.AnnualQuota`.
- הטסט שמוכיח את התיקון: `Create_ExceedingRemainingQuota_ReturnsBadRequest` ב-`backend/tests/LeaveManagement.Tests/LeaveRequestsTests.cs` — יוצר עובד עם מכסה של 10 ימים, מוסיף בקשה מאושרת בת 8 ימים, ואז מנסה להגיש בקשה נוספת של 3 ימים. מוודא שמתקבל `BadRequestObjectResult` ושלא נוצרה בקשה חדשה במסד הנתונים.

## 3. אישור בקשה (approve) ו‑concurrency
- איך טיפלתי במצבים לא חוקיים (כבר אושר / לא קיים):
- מה לגבי אישור של שתי בקשות במקביל:

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

</div>
