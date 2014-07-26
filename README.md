Patcher2
========

Small C# binary file patcher utility.<br>
The interesting bit is the byte pattern based search and replace.<br>
Should handle file sizes up to 2GB, and is really fast.<br>
(Created for educational porpoises.)

---

**Notes:**

Pattern format is like in 010 Editor, but Cheat Engine AOBScan format is also accepted.<br>
Replace is also a pattern, like search.<br>
Offset means the offset compared to the beginning location, of where the pattern is found.<br>
Offset can be negative, and is in base 10, while the patterns are in base 16.<br>
Adding ? -s to the beginning of replace patterns is equivalent to increasing the offset.<br>
(Adding ? -s to the beginning of search patterns is equivalent to decreasing the offset.)

---

**License:**<br>
LGPL (<http://wikipedia.org/wiki/GNU_Lesser_General_Public_License>)