<?xml version="1.0" encoding="windows-1250" ?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="1.0">

    <xsl:variable name="result_lang" select="/*[position()=1]/@lang" />
    <xsl:variable name="common_file" select="concat('common.', $result_lang, '.xml')" />
    <xsl:variable name="page_id" select="/*[position()=1]/@id" />
    <xsl:variable name="common" select="document($common_file)" />
    <xsl:param name="file_extension">xml</xsl:param>
    <xsl:param name="sourceforge">0</xsl:param>

    <xsl:template match="/">
        <html>
            <head>
                <link rel="stylesheet" href="../style.css" type="text/css" />
                <title>Sooda</title>
            </head>
            <body width="100%">
                <table align="center" class="page" cellpadding="0" cellspacing="0">
                    <tr>
                        <td class="header" colspan="2"><img src="../titlebanner.png" /></td>
                    </tr>
                    <tr>
                        <td valign="top" class="controls">
                            <xsl:call-template name="controls" />
                        </td>
                        <td valign="top" align="left" class="content">
                            <xsl:apply-templates select="content" />
                        </td>
                    </tr>
                    <tr>
                        <td class="hostedby">
                            <xsl:if test="$sourceforge='1'">
                                <a href="http://sourceforge.net"><img src="http://sourceforge.net/sflogo.php?group_id=71422&amp;type=1" width="88" height="31" border="0" alt="SourceForge.net Logo" /></a>
                            </xsl:if>
                        </td>
                        <td class="copyright">Copyright (c) 2003-2004 by Jaros�aw Kowalski</td>
                    </tr>
                </table>
            </body>
        </html>
    </xsl:template>

    <xsl:template match="@* | node()">
        <xsl:copy>
            <xsl:apply-templates select="@* | node()" />
        </xsl:copy>
    </xsl:template>

    <xsl:template match="content">
        <xsl:apply-templates select="*" />
    </xsl:template>

    <xsl:template name="controls">
        <xsl:apply-templates select="$common/common/navigation" />
    </xsl:template>

    <xsl:template match="navigation">
        <xsl:apply-templates select="nav" />
        <p/>
        <xsl:if test="$result_lang = 'en'">
            <a>
                <xsl:attribute name="href">../pl/<xsl:value-of select="$page_id" />.<xsl:value-of select="$file_extension" /></xsl:attribute>
                <img alt="Polish flag" title="Kliknij tutaj aby prze��czy� na j�zyk polski" class="thinborder" src="../lang_pl.gif" />
            </a>
        </xsl:if>
        <xsl:if test="$result_lang = 'pl'">
            <a><xsl:attribute name="href">../en/<xsl:value-of select="$page_id" />.<xsl:value-of select="$file_extension" /></xsl:attribute>
                <img alt="English flag" title="Click here to switch to English" class="thinborder" src="../lang_en.gif" /></a>
        </xsl:if>
    </xsl:template>
    
    <xsl:template match="nav">
        <xsl:choose>
            <xsl:when test="$page_id = @href"><a class="nav_selected"><xsl:value-of select="@label" /></a></xsl:when>
            <xsl:otherwise>
                <a class="nav"><xsl:attribute name="href"><xsl:value-of select="@href" />.<xsl:value-of select="$file_extension" /></xsl:attribute><xsl:value-of select="@label" /></a>
            </xsl:otherwise>
        </xsl:choose>
    </xsl:template>
</xsl:stylesheet>
